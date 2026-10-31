"""Run only in new directories. No commands target the surrounding source repository."""
import argparse
from datetime import datetime, timezone
import difflib
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--lab-root', required=True, type=Path)
    parser.add_argument('--evidence-directory', required=True, type=Path)
    args = parser.parse_args()
    lab, evidence = args.lab_root.resolve(), args.evidence_directory.resolve()
    if lab.exists() or evidence.exists():
        parser.error('Both output paths must be new; nothing is removed or reused.')
    if lab == evidence or lab in evidence.parents or evidence in lab.parents:
        parser.error('Lab and public evidence must be separate directory trees.')
    for name in ('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_CONFIG_COUNT',
                 'GIT_AUTHOR_NAME', 'GIT_AUTHOR_EMAIL', 'GIT_COMMITTER_NAME', 'GIT_COMMITTER_EMAIL'):
        if os.environ.get(name):
            parser.error(f'Unset {name} before running the isolated lab.')
    lab.mkdir(parents=True)
    evidence.mkdir(parents=True)
    fixtures = Path(__file__).resolve().parent / 'fixtures'
    dev, remote, receiver = lab / 'developer', lab / 'shared.git', lab / 'receiver'
    template, hooks = lab / 'empty-template', lab / 'lab-hooks'
    template.mkdir()
    hooks.mkdir()
    empty_config = lab / 'empty.gitconfig'
    empty_config.write_bytes(b'')
    env = os.environ.copy()
    env.update(GIT_CONFIG_GLOBAL=str(empty_config), GIT_CONFIG_NOSYSTEM='1',
               GIT_TERMINAL_PROMPT='0', PYTHONDONTWRITEBYTECODE='1', PYTHONUTF8='1')
    records, checks, snapshots = [], [], {}
    receipt = dict(passed=False, failure=None, recorded_at_utc=datetime.now(timezone.utc).isoformat(),
                   python_version=platform.python_version(), scope='Local bare remote; one developer; index exports; one clean receiver. Python teaching behavior only. No engine, real network failure, SVN/P4 end-to-end, binary splitting or production hooks.')

    def public(value):
        if isinstance(value, str):
            return value.replace(str(lab), '[LAB]').replace(lab.as_posix(), '[LAB]')
        if isinstance(value, list):
            return [public(v) for v in value]
        if isinstance(value, dict):
            return {k: public(v) for k, v in value.items()}
        return value

    def run(tool, arguments, directory, expected=(0,), input_text=None):
        command = [str(tool), *map(str, arguments)]
        # Binary stdin preserves patch LF on Windows; text mode translates it to CRLF.
        result = subprocess.run(command, cwd=directory, env=env,
                                input=None if input_text is None else input_text.encode('utf-8'),
                                capture_output=True)
        result.stdout = result.stdout.decode('utf-8', errors='replace').replace('\r\n', '\n')
        result.stderr = result.stderr.decode('utf-8', errors='replace').replace('\r\n', '\n')
        records.append(dict(tool='python' if str(tool) == sys.executable else str(tool),
                            arguments=list(map(str, arguments)), directory=str(directory),
                            stdin=input_text, exit_code=result.returncode,
                            stdout=result.stdout, stderr=result.stderr,
                            expected_exit_codes=list(expected)))
        if result.returncode not in expected:
            raise RuntimeError(f'Unexpected exit {result.returncode}: {command[0]} {arguments}: {result.stderr}')
        return result

    def git(directory, *arguments, expected=(0,), input_text=None):
        return run('git', ['-C', str(directory), *arguments], lab, expected, input_text).stdout.rstrip('\r\n')

    def check(condition, label):
        if not condition:
            raise AssertionError(label)
        checks.append(label)

    def write(directory, name, content):
        data = content.encode('utf-8') if isinstance(content, str) else content
        (directory / name).write_bytes(data)
        records.append(dict(action='write_fixture', path=str(directory / name),
                            sha256=hashlib.sha256(data).hexdigest(), size=len(data)))

    def inventory(directory):
        return {p.relative_to(directory).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                for p in sorted(directory.rglob('*'))
                if p.is_file() and '.git' not in p.relative_to(directory).parts}

    def preserve(label):
        current = inventory(dev)
        snapshots[label] = dict(files_sha256=current, status=git(dev, 'status', '--short'))
        check(current == original_inventory, f'{label}: developer bytes and path inventory retained')
        check(not (dev / 'obsolete.txt').exists(), f'{label}: B deletion retained')

    def behavior(directory, expected=(0,)):
        return run(sys.executable, ['-B', str(directory / 'skill.py')], directory, expected)

    def export_index(name):
        target = lab / name
        target.mkdir()
        git(dev, 'checkout-index', '--all', '--prefix=' + target.as_posix() + '/')
        return target

    try:
        receipt['git_version'] = git(lab, '--version')
        git(lab, 'init', '--bare', '--initial-branch=main', '--template=' + str(template), str(remote))
        git(lab, 'init', '--initial-branch=main', '--template=' + str(template), str(dev))
        for key, value in (('user.name', 'Version Management Lab'), ('user.email', 'lab@example.invalid'),
                           ('core.autocrlf', 'false'), ('core.eol', 'lf'), ('commit.gpgsign', 'false'),
                           ('core.hooksPath', str(hooks))):
            git(dev, 'config', '--local', key, value)
        baseline_code = (fixtures / 'baseline-skill.py').read_bytes()
        task_a_code = (fixtures / 'task-a-skill.py').read_bytes()
        working_code = (fixtures / 'working-skill.py').read_bytes()
        write(dev, 'skill.py', baseline_code)
        write(dev, 'notes.txt', 'baseline notes\n')
        write(dev, 'texture.bin', b'\x00\x01BASELINE\xff')
        write(dev, 'obsolete.txt', 'old content\n')
        git(dev, 'add', '--', 'skill.py', 'notes.txt', 'texture.bin', 'obsolete.txt')
        git(dev, 'commit', '-m', 'Baseline')
        baseline = git(dev, 'rev-parse', 'HEAD')
        git(dev, 'remote', 'add', 'origin', str(remote))
        git(dev, 'push', 'origin', baseline + ':refs/heads/main')
        check(git(dev, 'status', '--short') == '', 'baseline starts clean')
        write(dev, 'skill.py', working_code)
        write(dev, 'damage.json', (fixtures / 'damage.json').read_bytes())
        write(dev, 'notes.txt', 'B: unfinished charge notes\n')
        write(dev, 'texture.bin', b'\x00\x02B-EXPERIMENT\xfe')
        write(dev, 'scratch.txt', 'B: untracked draft\n')
        (dev / 'obsolete.txt').unlink()
        records.append(dict(action='delete_own_fixture', path=str(dev / 'obsolete.txt')))
        original_inventory = inventory(dev)
        snapshots['developer_before_selection'] = dict(files_sha256=original_inventory,
                                                       status=git(dev, 'status', '--short'))
        work_result = behavior(dev)
        check(json.loads(work_result.stdout) == dict(damage=80, charge=True), 'developer runs A+B with local dependency')

        patch = ''.join(difflib.unified_diff(baseline_code.decode().splitlines(keepends=True),
                                            task_a_code.decode().splitlines(keepends=True),
                                            fromfile='a/skill.py', tofile='b/skill.py'))
        (evidence / 'task-a.patch').write_bytes(patch.encode())
        git(dev, 'apply', '--cached', '--check', '-', input_text=patch)
        git(dev, 'apply', '--cached', '-', input_text=patch)
        check(git(dev, 'show', ':skill.py') == task_a_code.decode().rstrip('\n'), 'index script is A-only content')
        incomplete_tree = git(dev, 'write-tree')
        incomplete = export_index('candidate-missing-dependency')
        bad = behavior(incomplete, expected=(1,))
        check('FileNotFoundError' in bad.stderr and 'damage.json' in bad.stderr, 'clean candidate exposes omitted JSON dependency')
        check(not (incomplete / 'damage.json').exists(), 'incomplete export has no hidden JSON input')
        preserve('after_partial_selection')

        git(dev, 'add', '--', 'damage.json')
        candidate_tree = git(dev, 'write-tree')
        changed_paths = git(dev, 'diff', '--cached', '--name-only').splitlines()
        check(changed_paths == ['damage.json', 'skill.py'], 'candidate changes exactly A script and its dependency')
        check(git(dev, 'show', ':skill.py') == task_a_code.decode().rstrip('\n'), 'adding dependency leaves selected script unchanged')
        complete = export_index('candidate-complete')
        complete_result = behavior(complete)
        check(json.loads(complete_result.stdout) == dict(damage=80, charge=False), 'complete candidate runs A without B')
        check((complete / 'texture.bin').read_bytes() == b'\x00\x01BASELINE\xff', 'candidate keeps baseline binary rather than B bytes')
        check((complete / 'obsolete.txt').exists() and not (complete / 'scratch.txt').exists(), 'candidate excludes B deletion and untracked draft')
        preserve('after_complete_selection')
        snapshots['candidate'] = dict(incomplete_tree=incomplete_tree, tree=candidate_tree,
                                      changed_paths=changed_paths, files_sha256=inventory(complete),
                                      behavior=json.loads(complete_result.stdout))

        hook_path = hooks / 'pre-commit'
        write(hooks, 'pre-commit', '#!/bin/sh\nprintf "%s\\n" "VM-LAB: intentional rejection" >&2\nexit 1\n')
        hook_path.chmod(0o755)
        rejected = run('git', ['-C', str(dev), 'commit', '-m', 'Task A'], lab, expected=(1,))
        check('VM-LAB: intentional rejection' in rejected.stderr, 'own pre-commit hook rejects the commit')
        check(git(dev, 'rev-parse', 'HEAD') == baseline, 'rejected commit leaves HEAD at baseline')
        check(git(dev, 'write-tree') == candidate_tree, 'rejected commit retains candidate index tree')
        check(git(dev, 'ls-remote', '--heads', 'origin', 'refs/heads/main').split()[0] == baseline, 'remote remains baseline after rejection')
        preserve('after_rejection')
        hook_path.rename(hooks / 'pre-commit.disabled')
        records.append(dict(action='disable_own_lab_hook', path=str(hook_path)))
        git(dev, 'commit', '-m', 'Task A: damage configuration only')
        candidate_commit = git(dev, 'rev-parse', 'HEAD')
        check(git(dev, 'rev-parse', 'HEAD^') == baseline, 'successful commit has baseline parent')
        check(git(dev, 'rev-parse', 'HEAD^{tree}') == candidate_tree, 'successful commit records verified candidate tree')
        check(git(dev, 'ls-remote', '--heads', 'origin', 'refs/heads/main').split()[0] == baseline, 'local commit does not move remote ref')
        preserve('after_commit')

        # The controller retains true output for audit; the simulated caller receives only unknown.
        push_result = run('git', ['-C', str(dev), 'push', 'origin', candidate_commit + ':refs/heads/main'], lab)
        snapshots['lost_receipt'] = dict(caller_observed='unknown', simulation='wrapper discards successful push reply; not a real network failure',
                                        controller_push_exit=push_result.returncode)
        records.append(dict(action='simulate_discard_push_reply', caller_result='unknown'))
        remote_commit = git(dev, 'ls-remote', '--heads', 'origin', 'refs/heads/main').split()[0]
        check(remote_commit == candidate_commit, 'independent remote query resolves unknown as delivered')
        check(git(remote, 'rev-list', '--count', baseline + '..refs/heads/main') == '1', 'remote has exactly one new task commit')
        check(sum(1 for r in records if r.get('tool') == 'git' and 'push' in r.get('arguments', [])) == 2,
              'only baseline push and task push occurred; no retry after lookup')
        preserve('after_push_and_lookup')

        git(lab, 'clone', '--no-hardlinks', '--no-checkout', '--template=' + str(template), str(remote), str(receiver))
        git(receiver, 'config', '--local', 'core.autocrlf', 'false')
        git(receiver, '-c', 'advice.detachedHead=false', 'switch', '--detach', candidate_commit)
        receiver_result = behavior(receiver)
        check(json.loads(receiver_result.stdout) == dict(damage=80, charge=False), 'receiver runs A without B at exact commit')
        check(git(receiver, 'status', '--short') == '', 'receiver remains clean after behavior check')
        check(inventory(receiver) == inventory(complete), 'receiver bytes equal verified index export')
        snapshots['receiver'] = dict(commit=git(receiver, 'rev-parse', 'HEAD'), behavior=json.loads(receiver_result.stdout),
                                     files_sha256=inventory(receiver))
        receipt.update(passed=True, baseline_commit=baseline, candidate_commit=candidate_commit,
                       candidate_tree=candidate_tree, remote_commit=remote_commit)
    except Exception as exc:
        receipt['failure'] = f'{type(exc).__name__}: {exc}'
        raise
    finally:
        receipt.update(passed_checks=checks, snapshots=snapshots)
        for name, value in (('receipt.json', receipt), ('commands.json', records)):
            (evidence / name).write_bytes((json.dumps(public(value), ensure_ascii=False, indent=2) + '\n').encode('utf-8'))
    print(f'PASS: {len(checks)} checks; evidence: {evidence}')


if __name__ == '__main__':
    main()
