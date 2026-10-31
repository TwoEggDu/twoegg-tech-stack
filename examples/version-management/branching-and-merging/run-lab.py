"""Create fresh local teaching repositories; never target the surrounding project."""
import argparse
from datetime import datetime, timezone
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
        parser.error('Both output directories must be new; nothing is removed or reused.')
    if lab == evidence or lab in evidence.parents or evidence in lab.parents:
        parser.error('Lab and public evidence must be separate trees.')
    for name in ('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_CONFIG_COUNT', 'GIT_CONFIG_PARAMETERS',
                 'GIT_AUTHOR_NAME', 'GIT_AUTHOR_EMAIL', 'GIT_COMMITTER_NAME', 'GIT_COMMITTER_EMAIL'):
        if os.environ.get(name):
            parser.error(f'Unset {name} before running this isolated lab.')
    lab.mkdir(parents=True)
    evidence.mkdir(parents=True)
    template, hooks = lab / 'empty-template', lab / 'empty-hooks'
    template.mkdir()
    hooks.mkdir()
    config = lab / 'empty.gitconfig'
    config.write_bytes(b'')
    env = os.environ.copy()
    env.update(GIT_CONFIG_GLOBAL=str(config), GIT_CONFIG_NOSYSTEM='1', GIT_TERMINAL_PROMPT='0',
               PYTHONDONTWRITEBYTECODE='1', PYTHONUTF8='1')
    fixtures = Path(__file__).resolve().parent / 'fixtures'
    records, checks, snapshots = [], [], {}
    receipt = dict(passed=False, failure=None, recorded_at_utc=datetime.now(timezone.utc).isoformat(),
                   python_version=platform.python_version(),
                   scope='Four fresh local Git repositories and one local clean clone. Ordinary merge, one text conflict, synthetic integer damage contract. No network, production branch policy, engine, binary/rename conflicts, rebase/cherry-pick or SVN/P4 runtime validation.')

    def public(value):
        if isinstance(value, str):
            return value.replace(str(lab), '[LAB]').replace(lab.as_posix(), '[LAB]')
        if isinstance(value, list):
            return [public(v) for v in value]
        if isinstance(value, dict):
            return {k: public(v) for k, v in value.items()}
        return value

    def run(tool, arguments, directory, expected=(0,)):
        result = subprocess.run([str(tool), *map(str, arguments)], cwd=directory, env=env, capture_output=True)
        result.stdout = result.stdout.decode('utf-8', errors='replace').replace('\r\n', '\n')
        result.stderr = result.stderr.decode('utf-8', errors='replace').replace('\r\n', '\n')
        records.append(dict(tool='python' if str(tool) == sys.executable else str(tool),
                            arguments=list(map(str, arguments)), directory=str(directory),
                            exit_code=result.returncode, expected_exit_codes=list(expected),
                            stdout=result.stdout, stderr=result.stderr))
        if result.returncode not in expected:
            raise RuntimeError(f'Unexpected exit {result.returncode}: {tool} {arguments}: {result.stderr}')
        return result

    def git(repo, *arguments, expected=(0,)):
        return run('git', ['-C', str(repo), *arguments], lab, expected).stdout.rstrip('\r\n')

    def check(condition, label):
        if not condition:
            raise AssertionError(label)
        checks.append(label)

    def write(repo, name, content):
        data = content.encode('utf-8') if isinstance(content, str) else content
        (repo / name).write_bytes(data)
        records.append(dict(action='write_teaching_fixture', path=str(repo / name),
                            sha256=hashlib.sha256(data).hexdigest(), size=len(data)))

    def commit(repo, message, *paths):
        git(repo, 'add', '--', *paths)
        git(repo, 'commit', '-m', message)
        return git(repo, 'rev-parse', 'HEAD')

    def initialize(name, files):
        repo = lab / name
        git(lab, 'init', '--initial-branch=main', '--template=' + str(template), str(repo))
        for key, value in (('user.name', 'Version Management Lab'), ('user.email', 'lab@example.invalid'),
                           ('core.autocrlf', 'false'), ('core.eol', 'lf'), ('commit.gpgsign', 'false'),
                           ('core.hooksPath', str(hooks)), ('rerere.enabled', 'false'), ('merge.ff', 'true')):
            git(repo, 'config', '--local', key, value)
        for name, content in files.items():
            write(repo, name, content)
        baseline = commit(repo, 'Baseline', *files)
        check(git(repo, 'status', '--short') == '', f'{repo.name}: clean baseline')
        return repo, baseline

    def snapshot(repo):
        return dict(head=git(repo, 'rev-parse', 'HEAD'), tree=git(repo, 'rev-parse', 'HEAD^{tree}'),
                    parents=git(repo, 'show', '-s', '--format=%P', 'HEAD').split(),
                    status=git(repo, 'status', '--short'),
                    graph=git(repo, 'log', '--graph', '--oneline', '--decorate', '--all'))

    def ancestry(repo, ancestor, target, label):
        git(repo, 'merge-base', '--is-ancestor', ancestor, target)
        checks.append(label)

    def inventory(repo):
        return {p.relative_to(repo).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                for p in sorted(repo.rglob('*')) if p.is_file() and '.git' not in p.relative_to(repo).parts}

    def behavior(repo, expected=(0,)):
        result = run(sys.executable, ['-B', str(repo / 'check_damage.py')], repo, expected)
        return json.loads(result.stdout), result

    try:
        receipt['git_version'] = git(lab, '--version')
        receipt['configuration'] = dict(merge_ff=True, rerere=False, global_system_config='isolated', hooks='empty')
        ff, ff_base = initialize('fast-forward', {'damage.rules': 'damage=100\n'})
        git(ff, 'switch', '-c', 'feature')
        write(ff, 'damage.rules', 'damage=80\n')
        ff_feature = commit(ff, 'Feature damage', 'damage.rules')
        git(ff, 'switch', 'main')
        check(git(ff, 'merge-base', 'main', 'feature') == ff_base, 'fast-forward: merge base is original main')
        count_before = git(ff, 'rev-list', '--all', '--count')
        git(ff, 'merge', '--ff-only', 'feature')
        s = snapshot(ff)
        check(s['head'] == ff_feature, 'fast-forward: main points to existing feature commit')
        check(git(ff, 'rev-list', '--all', '--count') == count_before, 'fast-forward: no additional commit created')
        check(s['parents'] == [ff_base] and s['status'] == '', 'fast-forward: existing single parent and clean worktree')
        check((ff / 'damage.rules').read_text() == 'damage=80\n', 'fast-forward: target contains feature content')
        snapshots['fast_forward'] = dict(baseline=ff_base, feature=ff_feature, **s)

        div, div_base = initialize('divergent', {'damage.rules': 'damage=100\n', 'charge.rules': 'charge=off\n'})
        git(div, 'branch', 'feature')
        write(div, 'damage.rules', 'damage=80\n')
        div_main = commit(div, 'Main damage', 'damage.rules')
        git(div, 'switch', 'feature')
        write(div, 'charge.rules', 'charge=on\n')
        div_feature = commit(div, 'Feature charge', 'charge.rules')
        git(div, 'switch', 'main')
        check(git(div, 'merge-base', 'main', 'feature') == div_base, 'divergent: merge base is shared baseline')
        git(div, 'merge', '--no-edit', 'feature')
        s = snapshot(div)
        check(s['parents'] == [div_main, div_feature], 'divergent: merge records both exact branch tips as parents')
        check(s['head'] not in (div_main, div_feature), 'divergent: merge creates a new commit')
        check((div / 'damage.rules').read_text() == 'damage=80\n' and (div / 'charge.rules').read_text() == 'charge=on\n', 'divergent: merged files contain both changes')
        ancestry(div, div_feature, 'main', 'divergent: source tip is reachable from target')
        check(s['status'] == '' and git(div, 'rev-parse', 'feature') == div_feature, 'divergent: worktree clean and source ref unchanged')
        snapshots['divergent_merge'] = dict(baseline=div_base, main_before=div_main, feature=div_feature, **s)

        conflict, conflict_base = initialize('text-conflict', {'damage.rules': 'damage=100\n'})
        git(conflict, 'branch', 'feature')
        write(conflict, 'damage.rules', 'damage=80\n')
        conflict_main = commit(conflict, 'Main damage 80', 'damage.rules')
        git(conflict, 'switch', 'feature')
        write(conflict, 'damage.rules', 'damage=90\n')
        conflict_feature = commit(conflict, 'Feature damage 90', 'damage.rules')
        git(conflict, 'switch', 'main')
        run('git', ['-C', str(conflict), 'merge', '--no-edit', 'feature'], lab, expected=(1,))
        stages = {str(i): git(conflict, 'show', f':{i}:damage.rules') for i in (1, 2, 3)}
        unmerged = git(conflict, 'ls-files', '-u')
        work = (conflict / 'damage.rules').read_text()
        check(git(conflict, 'rev-parse', 'HEAD') == conflict_main, 'conflict: HEAD does not advance')
        check(git(conflict, 'rev-parse', 'MERGE_HEAD') == conflict_feature, 'conflict: MERGE_HEAD identifies source')
        check(stages == {'1': 'damage=100', '2': 'damage=80', '3': 'damage=90'}, 'conflict: index stages retain base, current and source content')
        check(len(unmerged.splitlines()) == 3 and '<<<<<<<' in work and '>>>>>>>' in work, 'conflict: unmerged entries and working markers present')
        snapshots['text_conflict_before_resolution'] = dict(baseline=conflict_base, main_before=conflict_main,
                                                          feature=conflict_feature, stages=stages, unmerged=unmerged, working=work)
        write(conflict, 'damage.rules', 'damage=80\n')
        resolved = commit(conflict, 'Resolve according to teaching spec: damage 80', 'damage.rules')
        s = snapshot(conflict)
        check(s['parents'] == [conflict_main, conflict_feature], 'conflict: resolved merge has both expected parents')
        check(git(conflict, 'ls-files', '-u') == '' and s['status'] == '', 'conflict: resolved index and worktree are clean')
        check((conflict / 'damage.rules').read_text() == 'damage=80\n', 'conflict: final content matches predeclared value 80')
        check(git(conflict, 'rev-parse', 'feature') == conflict_feature, 'conflict: source ref remains unchanged after resolution')
        snapshots['text_conflict_resolved'] = dict(resolved=resolved, **s)

        semantic, semantic_base = initialize('semantic', {
            'base.json': '{"damage": 100}\n', 'charge.json': '{"percent": 100}\n',
            'damage.py': (fixtures / 'damage.py').read_bytes(),
            'check_damage.py': (fixtures / 'check_damage.py').read_bytes()})
        checker_hash = inventory(semantic)['check_damage.py']
        base_behavior, _ = behavior(semantic)
        check(base_behavior['raw'] == base_behavior['final'] == 100, 'semantic: baseline satisfies fixed contract at 100')
        git(semantic, 'branch', 'feature')
        write(semantic, 'base.json', '{"damage": 120}\n')
        semantic_main = commit(semantic, 'Main base damage 120', 'base.json')
        main_behavior, _ = behavior(semantic)
        check(main_behavior['raw'] == main_behavior['final'] == 120, 'semantic: main alone passes at 120')
        git(semantic, 'switch', 'feature')
        write(semantic, 'charge.json', '{"percent": 140}\n')
        semantic_feature = commit(semantic, 'Feature charge 140 percent', 'charge.json')
        feature_behavior, _ = behavior(semantic)
        check(feature_behavior['raw'] == feature_behavior['final'] == 140, 'semantic: feature alone passes at 140')
        git(semantic, 'switch', 'main')
        check(git(semantic, 'merge-base', 'main', 'feature') == semantic_base, 'semantic: both changes start at shared baseline')
        git(semantic, 'merge', '--no-edit', 'feature')
        bad_merge = git(semantic, 'rev-parse', 'HEAD')
        bad_behavior, bad_result = behavior(semantic, expected=(1,))
        check(git(semantic, 'ls-files', '-u') == '' and git(semantic, 'status', '--short') == '', 'semantic: automatic merge has no text conflict and is clean')
        check(bad_behavior['base'] == 120 and bad_behavior['percent'] == 140 and bad_behavior['raw'] == bad_behavior['final'] == 168, 'semantic: combined behavior is 168 with both configurations retained')
        check('FINAL_DAMAGE_OUT_OF_RANGE' in bad_result.stderr, 'semantic: unchanged behavior checker rejects successful merge')
        ancestry(semantic, semantic_main, 'main', 'semantic: original main tip is reachable even though behavior fails')
        ancestry(semantic, semantic_feature, 'main', 'semantic: source tip is reachable even though behavior fails')
        bad_snapshot = snapshot(semantic)
        check(bad_snapshot['parents'] == [semantic_main, semantic_feature], 'semantic: failing merge still records correct parents')
        snapshots['semantic_merged_invalid'] = dict(baseline=semantic_base, main_before=semantic_main,
                                                  feature=semantic_feature, baseline_behavior=base_behavior,
                                                  main_behavior=main_behavior, feature_behavior=feature_behavior,
                                                  behavior=bad_behavior, **bad_snapshot)
        write(semantic, 'damage.py', (fixtures / 'capped-damage.py').read_bytes())
        fixed = commit(semantic, 'Apply predeclared cap while preserving both configurations', 'damage.py')
        fixed_behavior, _ = behavior(semantic)
        check(fixed_behavior == dict(base=120, percent=140, raw=168, final=150, cap=150), 'semantic: adaptation preserves both changes and caps only final damage')
        check(inventory(semantic)['check_damage.py'] == checker_hash, 'semantic: checker bytes were not relaxed to make test pass')
        check(git(semantic, 'show', '--format=', '--name-only', fixed).splitlines() == ['damage.py'], 'semantic: adaptation commit changes only calculation program')
        check(git(semantic, 'show', '-s', '--format=%P', fixed) == bad_merge, 'semantic: adaptation keeps failing merge visible in history')
        check(git(semantic, 'rev-parse', 'feature') == semantic_feature, 'semantic: source ref remains at original feature commit')
        snapshots['semantic_fixed'] = dict(behavior=fixed_behavior, files_sha256=inventory(semantic), **snapshot(semantic))

        receiver = lab / 'receiver'
        git(lab, 'clone', '--no-hardlinks', '--no-checkout', '--template=' + str(template), str(semantic), str(receiver))
        git(receiver, 'config', '--local', 'core.autocrlf', 'false')
        git(receiver, '-c', 'advice.detachedHead=false', 'switch', '--detach', fixed)
        received_behavior, _ = behavior(receiver)
        check(received_behavior == fixed_behavior, 'receiver: fixed exact commit passes same contract')
        check(git(receiver, 'rev-parse', 'HEAD') == fixed and git(receiver, 'status', '--short') == '', 'receiver: exact identity and clean worktree')
        check(inventory(receiver) == inventory(semantic), 'receiver: all tracked file bytes match source target')
        snapshots['receiver'] = dict(behavior=received_behavior, files_sha256=inventory(receiver), **snapshot(receiver))
        receipt.update(passed=True, semantic_invalid_merge=bad_merge, semantic_fixed_commit=fixed)
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
