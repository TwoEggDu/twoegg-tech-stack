"""RenderDoc 1.43: inspect the existing Wall_1 capture pair, without launching Piccolo.

Run qrenderdoc --python with this directory as cwd. The original RDC files remain local.
PICCOLO_GBUFFER_LAB selects the lab; PICCOLO_GBUFFER_REPORT selects the prior vertex report.
"""
import renderdoc as rd
import hashlib
import json
import math
import os
import struct
import sys
import tempfile
import traceback

base = os.environ.get('PICCOLO_GBUFFER_LAB', os.path.join(tempfile.gettempdir(), 'PiccoloDebug20261026'))
prior_path = os.environ.get('PICCOLO_GBUFFER_REPORT', os.path.join(os.getcwd(), 'gbuffer-replay-report.json'))
report = {'renderdoc_version': rd.GetVersionString(), 'scope': 'Offline capture replay; GBuffer and deferred lighting only'}
SEMANTICS = set(['in_world_position', 'in_texcoord', 'in_normal', 'in_tangent',
                 'gbuffer', 'gbuffer_a', 'gbuffer_b', 'gbuffer_c',
                 'out_gbuffer_a', 'out_gbuffer_b', 'out_gbuffer_c',
                 'scene_depth', 'Lo', 'La', 'Libl', 'result_color', 'out_color',
                 'V', 'F0', 'NoL', 'L', 'shadow', 'closest_depth', 'current_depth'])


def scalar(obj):
    result = {}
    for name in dir(obj):
        if name.startswith('_'):
            continue
        try:
            value = getattr(obj, name)
            if isinstance(value, (int, float, str, bool)):
                result[name] = value
            elif isinstance(value, rd.ResourceId):
                result[name] = str(value)
        except Exception:
            pass
    return result


def variable(obj):
    count = obj.rows * obj.columns
    values = list(obj.value.u32v)[:count] if obj.type == rd.VarType.UInt else list(obj.value.f32v)[:count]
    return {'id': obj.name, 'type': str(obj.type), 'rows': obj.rows, 'columns': obj.columns,
            'value': values, 'members': [variable(member) for member in obj.members]}


def multiply(matrix, vector):
    return [sum(matrix[row * 4 + col] * vector[col] for col in range(4)) for row in range(4)]


def spirv_names(reflection):
    data = bytes(reflection.rawBytes)
    words = struct.unpack('<' + 'I' * (len(data) // 4), data)
    result = {}
    offset = 5
    while offset < len(words):
        size, opcode = words[offset] >> 16, words[offset] & 65535
        assert size > 0
        if opcode == 5:  # OpName from this capture's original SPIR-V, not assumed IDs.
            name = struct.pack('<' + 'I' * (size - 2), *words[offset + 2:offset + size]).split(b'\0')[0].decode('utf-8')
            result['_' + str(words[offset + 1])] = name
        offset += size
    return result, hashlib.sha256(data).hexdigest()


def debug_pixel(ctrl, event, x, y):
    ctrl.SetFrameEvent(event, True)
    names, fingerprint = spirv_names(ctrl.GetPipelineState().GetShaderReflection(rd.ShaderStage.Fragment))
    trace = ctrl.DebugPixel(x, y, rd.DebugPixelInputs())
    assert trace and trace.debugger, 'Pixel shader debugger unavailable'
    inputs = [variable(value) for value in trace.inputs]
    latest = {value['id']: value for value in inputs}
    steps = 0
    try:
        for unused in range(1000):
            states = ctrl.ContinueDebug(trace.debugger)
            if not states:
                break
            steps += len(states)
            for state in states:
                for change in state.changes:
                    if change.after.name:
                        latest[change.after.name] = variable(change.after)
        else:
            raise RuntimeError('Shader debug step cap exceeded')
        semantics = {}
        for ident, name in names.items():
            if name in SEMANTICS:
                semantics.setdefault(name, []).append({'id': ident, 'observed': ident in latest, 'data': latest.get(ident)})
        return {'event': event, 'steps': steps, 'spirv_sha256': fingerprint,
                'inputs': [{'source_name': names.get(value['id']), 'data': value} for value in inputs],
                'semantics': semantics}
    finally:
        ctrl.FreeTrace(trace)


def observed(debug, name):
    values = [value['data'] for value in debug['semantics'].get(name, []) if value['observed']]
    assert len(values) == 1, (name, len(values))
    return values[0]


def triangles(vertices, indices, viewport, x, y):
    result = []
    for tri in range(len(indices) // 3):
        rows = [vertices[index] for index in indices[tri * 3:tri * 3 + 3]]
        if min(row['clip'][3] for row in rows) <= 0:
            continue
        points = [(viewport['x'] + (row['clip'][0] / row['clip'][3] + 1) * viewport['width'] / 2,
                   viewport['y'] + (row['clip'][1] / row['clip'][3] + 1) * viewport['height'] / 2,
                   row['clip'][2] / row['clip'][3]) for row in rows]
        (x0, y0, z0), (x1, y1, z1), (x2, y2, z2) = points
        denominator = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denominator) < 1e-8:
            continue
        a = ((y1 - y2) * (x + .5 - x2) + (x2 - x1) * (y + .5 - y2)) / denominator
        b = ((y2 - y0) * (x + .5 - x2) + (x0 - x2) * (y + .5 - y2)) / denominator
        bary = [a, b, 1 - a - b]
        if min(bary) < 0:
            continue
        weights = [weight / row['clip'][3] for weight, row in zip(bary, rows)]
        total = sum(weights)
        result.append({'triangle': tri, 'screen_triangle': points, 'area': abs(denominator) / 2,
                       'barycentric': bary, 'predicted_depth': sum(weight * point[2] for weight, point in zip(bary, points)),
                       'predicted_world': [sum(weight * row['world'][axis] for weight, row in zip(weights, rows)) / total for axis in range(3)],
                       'predicted_uv': [sum(weight * row['uv'][axis] for weight, row in zip(weights, rows)) / total for axis in range(2)]})
    return result


def choose_pixel(target, viewport, depth_data, width, height):
    candidates = []
    for tri in range(len(target['indices']) // 3):
        rows = [target['vertices'][index] for index in target['indices'][tri * 3:tri * 3 + 3]]
        if min(row['clip'][3] for row in rows) <= 0:
            continue
        x = int(sum(viewport['x'] + (row['clip'][0] / row['clip'][3] + 1) * viewport['width'] / 2 for row in rows) / 3)
        y = int(sum(viewport['y'] + (row['clip'][1] / row['clip'][3] + 1) * viewport['height'] / 2 for row in rows) / 3)
        if not (0 <= x < width and 0 <= y < height and viewport['x'] <= x < viewport['x'] + viewport['width'] and viewport['y'] <= y < viewport['y'] + viewport['height']):
            continue
        for candidate in triangles(target['vertices'], target['indices'], viewport, x, y):
            if candidate['triangle'] != tri or min(candidate['barycentric']) < .1:
                continue
            candidate['xy'] = [x, y]
            candidate['actual_depth'] = struct.unpack_from('<f', depth_data, (y * width + x) * 4)[0]
            candidate['depth_error'] = abs(candidate['actual_depth'] - candidate['predicted_depth'])
            if candidate['depth_error'] < 1e-5:
                candidates.append(candidate)
    assert candidates, 'No depth-confirmed target surface candidate'
    return max(candidates, key=lambda value: value['area'])


def read_pixel(ctrl, descriptor, x, y, textures):
    tex = textures[str(descriptor.resource)]
    data = bytes(ctrl.GetTextureData(descriptor.resource, rd.Subresource()))
    name = tex.format.Name()
    stride = {'R8G8B8A8_UNORM': 4, 'R8G8B8A8_SRGB': 4, 'D32': 4, 'R16G16B16A16_FLOAT': 8}[name]
    raw = data[(y * tex.width + x) * stride:(y * tex.width + x + 1) * stride]
    picked = list(ctrl.PickPixel(descriptor.resource, x, y, rd.Subresource(), rd.CompType.Typeless).floatValue)
    values = list(struct.unpack('<4e', raw)) if stride == 8 else list(struct.unpack('<f', raw)) if name == 'D32' else list(raw)
    return {'resource': str(descriptor.resource), 'view': str(descriptor.view), 'format': name,
            'raw_hex': raw.hex(), 'raw_components': values, 'picked_float': picked}


def sample(ctrl, target, viewport, xy, lighting_event, textures):
    x, y = xy
    ctrl.SetFrameEvent(target['event'], True)
    pipe = ctrl.GetPipelineState()
    outputs = pipe.GetOutputTargets()[:3]
    depth = pipe.GetDepthTarget()
    history = []
    for modification in ctrl.PixelHistory(outputs[0].resource, x, y, rd.Subresource(), rd.CompType.Typeless):
        valid = {}
        for name in ['preMod', 'postMod', 'shaderOut']:
            try:
                valid[name] = getattr(modification, name).IsValid()
            except Exception:
                valid[name] = None
        history.append({'event': modification.eventId, 'reported_primitive': modification.primitiveID,
                        'passed': modification.Passed(), 'value_validity': valid})
    ctrl.SetFrameEvent(target['event'], True)
    result = {'xy': xy, 'history_event_records': history,
              'gbuffer': [read_pixel(ctrl, output, x, y, textures) for output in outputs],
              'depth': read_pixel(ctrl, depth, x, y, textures)}
    debug = debug_pixel(ctrl, target['event'], x, y)
    result['gbuffer_debug'] = debug
    actual_world = observed(debug, 'in_world_position')['value']
    hits = []
    for slot, matrix in enumerate(target['all_instance_matrices']):
        vertices = []
        for vertex in target['vertices']:
            world = multiply(matrix, vertex['input_position'] + [1])
            vertices.append({'world': world[:3], 'clip': multiply(target['perframe_matrix'], world), 'uv': vertex['uv']})
        for hit in triangles(vertices, target['indices'], viewport, x, y):
            hit['instance_slot'] = slot
            hit['world_error'] = max(abs(a - b) for a, b in zip(hit['predicted_world'], actual_world))
            hits.append(hit)
    hits.sort(key=lambda hit: hit['world_error'])
    result['instance_attribution'] = hits
    result['confirmed_slots'] = sorted(set(hit['instance_slot'] for hit in hits if hit['world_error'] < 1e-3))
    ctrl.SetFrameEvent(lighting_event, True)
    pipe = ctrl.GetPipelineState()
    vk = ctrl.GetVulkanPipelineState()
    sets = vk.graphics.descriptorSets
    resources = pipe.GetReadOnlyResources(rd.ShaderStage.Fragment) + pipe.GetReadWriteResources(rd.ShaderStage.Fragment)
    descriptors = [{'access': scalar(value.access), 'descriptor': scalar(value.descriptor)} for value in resources]
    inputs = [value for value in resources if value.access.descriptorStore == sets[1].descriptorSetResourceId]
    inputs.sort(key=lambda value: value.access.byteOffset)
    assert [value.descriptor.resource for value in inputs] == [output.resource for output in outputs] + [depth.resource]
    perframe = next(value.descriptor for value in resources if value.access.descriptorStore == sets[0].descriptorSetResourceId and value.access.byteOffset == 0)
    perframe_bytes = bytes(ctrl.GetBufferData(perframe.resource, perframe.byteOffset, perframe.byteSize))
    assert len(perframe_bytes) == 688
    reflection = pipe.GetShaderReflection(rd.ShaderStage.Fragment)
    layouts = [{member.name: member.byteOffset for member in resource.variableType.members}
               for resource in reflection.readOnlyResources + reflection.readWriteResources]
    layout = next(value for value in layouts if 'point_light_num' in value)
    for name, offset in [('camera_position', 64), ('ambient_light', 80), ('point_light_num', 96),
                         ('scene_directional_light', 592), ('directional_light_proj_view', 624)]:
        assert layout[name] == offset, (name, layout[name])
    light = {'event': lighting_event, 'pipeline': str(vk.graphics.pipelineResourceId), 'shader': str(vk.fragmentShader.resourceId),
             'subpass': vk.currentPass.renderpass.subpass, 'viewport': scalar(pipe.GetViewport(0)),
             'sets': [str(value.descriptorSetResourceId) for value in sets], 'descriptors': descriptors,
             'inputs': [read_pixel(ctrl, value.descriptor, x, y, textures) for value in inputs],
             'output': read_pixel(ctrl, pipe.GetOutputTargets()[0], x, y, textures),
             'perframe': {'descriptor': scalar(perframe), 'member_offsets': layout, 'sha256': hashlib.sha256(perframe_bytes).hexdigest(),
                          'camera': list(struct.unpack_from('<3f', perframe_bytes, 64)),
                          'ambient': list(struct.unpack_from('<3f', perframe_bytes, 80)),
                          'point_light_count': struct.unpack_from('<I', perframe_bytes, 96)[0],
                          'directional_direction': list(struct.unpack_from('<3f', perframe_bytes, 592)),
                          'directional_color': list(struct.unpack_from('<3f', perframe_bytes, 608))}}
    debug = debug_pixel(ctrl, lighting_event, x, y)
    light['debug'] = debug
    components = [observed(debug, name)['value'] for name in ['Lo', 'La', 'Libl']]
    shader_result = observed(debug, 'result_color')['value']
    light['sum_error'] = max(abs(sum(value[axis] for value in components) - shader_result[axis]) for axis in range(3))
    stored = light['output']['raw_components']
    bits = struct.unpack('<4H', bytes.fromhex(light['output']['raw_hex']))
    ulps = [2 ** (((word >> 10) & 31) - 25) if ((word >> 10) & 31) else 2 ** -24 for word in bits[:3]]
    light['debug_to_stored_abs_error'] = [abs(a - b) for a, b in zip(shader_result, stored)]
    light['debug_to_stored_half_ulp_error'] = [abs(a - b) / ulp for a, b, ulp in zip(shader_result, stored, ulps)]
    light['gbuffer_input_match'] = all(source['raw_hex'] == dest['raw_hex'] for source, dest in zip(result['gbuffer'] + [result['depth']], light['inputs']))
    light['debug_input_match'] = all(observed(debug, name)['value'] == pixel['picked_float']
                                    for name, pixel in zip(['gbuffer_a', 'gbuffer_b', 'gbuffer_c'], result['gbuffer']))
    light['debug_input_match'] = light['debug_input_match'] and observed(debug, 'scene_depth')['value'] == result['depth']['raw_components']
    assert light['output']['raw_components'] == light['output']['picked_float']
    result['lighting'] = light
    return result


def walk(ctrl, actions, path=''):
    for action in actions:
        name = action.GetName(ctrl.GetStructuredFile())
        yield action, path
        for child in walk(ctrl, action.children, path + '/' + name):
            yield child


try:
    prior = json.load(open(prior_path))
    for phase in ['before', 'after']:
        capture = prior[phase]
        target = capture['matches'][0]
        filename = os.path.join(base, 'captures', capture['file'])
        assert hashlib.sha256(open(filename, 'rb').read()).hexdigest() == capture['sha256']
        cap = rd.OpenCaptureFile()
        assert cap.OpenFile(filename, '', None) == rd.ResultCode.Succeeded
        status, ctrl = cap.OpenCapture(rd.ReplayOptions(), None)
        assert status == rd.ResultCode.Succeeded, str(status)
        try:
            assert ctrl.GetAPIProperties().shaderDebugging
            textures = {str(texture.resourceId): texture for texture in ctrl.GetTextures()}
            lighting = [action.eventId for action, path in walk(ctrl, ctrl.GetRootActions())
                        if 'Deferred Lighting' in path and action.flags & rd.ActionFlags.Drawcall]
            assert len(lighting) == 1, lighting
            ctrl.SetFrameEvent(target['event'], True)
            pipe = ctrl.GetPipelineState()
            viewport = scalar(pipe.GetViewport(0))
            depth = pipe.GetDepthTarget().resource
            tex = textures[str(depth)]
            candidate = choose_pixel(target, viewport, bytes(ctrl.GetTextureData(depth, rd.Subresource())), tex.width, tex.height)
            result = {'file': capture['file'], 'sha256': capture['sha256'], 'gbuffer_event': target['event'],
                      'viewport': viewport, 'candidate': candidate,
                      'sample': sample(ctrl, target, viewport, candidate['xy'], lighting[0], textures)}
            assert result['sample']['confirmed_slots'] == [target['instance_slot']], result['sample']['confirmed_slots']
            report[phase] = result
            if phase == 'after':
                result['previous_pixel_reference'] = sample(ctrl, target, viewport, report['before']['candidate']['xy'], lighting[0], textures)
        finally:
            ctrl.Shutdown()
            cap.Shutdown()
    report['checks'] = {}
    for phase in ['before', 'after']:
        light = report[phase]['sample']['lighting']
        assert light['gbuffer_input_match']
        assert light['debug_input_match']
        assert light['sum_error'] < 1e-6
        assert max(light['debug_to_stored_half_ulp_error']) < 2
        report['checks'][phase] = {'target_slot': report[phase]['sample']['confirmed_slots'],
                                   'input_attachment_bytes_match': light['gbuffer_input_match'],
                                   'debug_inputs_match_readback': light['debug_input_match'],
                                   'Lo_La_Libl_sum_error': light['sum_error'],
                                   'debug_to_stored_half_ulp_max': max(light['debug_to_stored_half_ulp_error'])}
except Exception:
    report['error'] = traceback.format_exc()
with open(os.path.join(base, 'gbuffer-lighting-report.json'), 'w') as output:
    json.dump(report, output, indent=2)
sys.exit(0)
