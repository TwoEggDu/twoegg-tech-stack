import renderdoc as rd
import struct,json,sys,os,hashlib,traceback,tempfile
base=os.environ.get('PICCOLO_GBUFFER_LAB',os.path.join(tempfile.gettempdir(),'PiccoloDebug20261026'))
report={'renderdoc_version':rd.GetVersionString(),'scope':'Replay-derived binding data and post-VS output; capture frames differ from CDB sample'}
def fields(o):return [n for n in dir(o) if not n.startswith('_') and n not in ['acquire','append','disown','next','own','this','thisown']]
def scalar(o):
 d={}
 for n in fields(o):
  try:
   v=getattr(o,n)
   if isinstance(v,(int,float,str,bool)):d[n]=v
   elif isinstance(v,rd.ResourceId):d[n]=str(v)
  except:pass
 return d
def members(t,depth=0):
 return [{'name':m.name,'byteOffset':m.byteOffset,'type':scalar(m.type),'children':members(m.type,depth+1) if depth<2 else []} for m in t.members]
def mul(m,v):return [sum(m[r*4+c]*v[c] for c in range(4)) for r in range(4)]
def walk(ctrl,acts,path):
 for a in acts:
  n=a.GetName(ctrl.GetStructuredFile())
  if 'Mesh GBuffer' in path and a.flags & rd.ActionFlags.Drawcall:yield a,path
  for x in walk(ctrl,a.children,path+'/'+n):yield x

def extract(filename,expected_x):
 cap=rd.OpenCaptureFile();opened=cap.OpenFile(filename,'',None);assert opened==rd.ResultCode.Succeeded,str(opened)
 result,ctrl=cap.OpenCapture(rd.ReplayOptions(),None);assert result==rd.ResultCode.Succeeded,str(result)
 out={'file':os.path.basename(filename),'bytes':os.path.getsize(filename),'sha256':hashlib.sha256(open(filename,'rb').read()).hexdigest(),'draws':[],'matches':[]}
 for a,path in walk(ctrl,ctrl.GetRootActions(),''):
  ctrl.SetFrameEvent(a.eventId,True);p=ctrl.GetPipelineState();vk=ctrl.GetVulkanPipelineState();sets=vk.graphics.descriptorSets
  buffers=p.GetReadWriteResources(rd.ShaderStage.Vertex)+p.GetReadOnlyResources(rd.ShaderStage.Vertex)
  by_binding={x.access.byteOffset:x for x in buffers if x.access.descriptorStore==sets[0].descriptorSetResourceId}
  if 1 not in by_binding:continue
  desc=by_binding[1].descriptor;data=bytes(ctrl.GetBufferData(desc.resource,desc.byteOffset,a.numInstances*80))
  mats=[list(struct.unpack_from('<16f',data,i*80+16)) for i in range(a.numInstances)]
  candidates=[i for i,m in enumerate(mats) if abs(m[3]-expected_x)<1e-5 and abs(m[7]+0.38554)<1e-5 and abs(m[11])<1e-5]
  draw={'event':a.eventId,'path':path,'indices':a.numIndices,'instances':a.numInstances,'pipeline':str(vk.graphics.pipelineResourceId),'vertex_shader':str(vk.vertexShader.resourceId),'fragment_shader':str(vk.fragmentShader.resourceId),'descriptor':scalar(desc),'translations':[[m[3],m[7],m[11]] for m in mats]};out['draws'].append(draw)
  for inst in candidates:
   pvdesc=by_binding[0].descriptor;pv=list(struct.unpack('<16f',bytes(ctrl.GetBufferData(pvdesc.resource,pvdesc.byteOffset,64))))
   mesh=ctrl.GetPostVSData(inst,0,rd.MeshDataStage.VSOut);assert not mesh.status,mesh.status
   idxdata=bytes(ctrl.GetBufferData(mesh.indexResourceId,mesh.indexByteOffset,mesh.numIndices*mesh.indexByteStride));fmt={1:'B',2:'H',4:'I'}[mesh.indexByteStride];idx=list(struct.unpack('<'+fmt*mesh.numIndices,idxdata));n=max(idx)+1
   post=bytes(ctrl.GetBufferData(mesh.vertexResourceId,mesh.vertexByteOffset,n*mesh.vertexByteStride));assert mesh.vertexByteStride==80,mesh.vertexByteStride
   vb=p.GetVBuffers()[0];vbbytes=bytes(ctrl.GetBufferData(vb.resourceId,vb.byteOffset,n*vb.byteStride));inputs=[list(struct.unpack_from('<3f',vbbytes,i*vb.byteStride)) for i in range(n)]
   rows=[list(struct.unpack_from('<20f',post,i*mesh.vertexByteStride)) for i in range(n)]
   verts=[{'vertex':i,'input_position':inputs[i],'clip':r[:4],'world':r[4:7],'normal':r[8:11],'tangent':r[12:15],'uv':r[16:18]} for i,r in enumerate(rows)]
   worlderr=max(abs(verts[i]['world'][j]-mul(mats[inst],inputs[i]+[1])[j]) for i in range(n) for j in range(3));cliperr=max(abs(v['clip'][j]-mul(pv,v['world']+[1])[j]) for v in verts for j in range(4))
   ib=p.GetIBuffer();ibbytes=bytes(ctrl.GetBufferData(ib.resourceId,ib.byteOffset+a.indexOffset*ib.byteStride,a.numIndices*ib.byteStride))
   reflection=vk.vertexShader.reflection
   target={'event':a.eventId,'instance_slot':inst,'draw':scalar(a),'pipeline':str(vk.graphics.pipelineResourceId),'vertex_shader':str(vk.vertexShader.resourceId),'fragment_shader':str(vk.fragmentShader.resourceId),'subpass':scalar(vk.currentPass.renderpass),'sets':[{'set':i,'descriptor_set':str(s.descriptorSetResourceId),'dynamic_offsets':[scalar(x) for x in s.dynamicOffsets]} for i,s in enumerate(sets)],'perframe_descriptor':scalar(pvdesc),'instance_descriptor':scalar(desc),'perframe_matrix':pv,'model_matrix':mats[inst],'all_instance_matrices':mats,'instance_flags':[struct.unpack_from('<f',data,i*80)[0] for i in range(a.numInstances)],'vbuffers':[scalar(x) for x in p.GetVBuffers()],'ibuffer':scalar(ib),'input_vertex_bytes_sha256':hashlib.sha256(vbbytes).hexdigest(),'index_bytes_sha256':hashlib.sha256(ibbytes).hexdigest(),'post_vs_format':scalar(mesh),'post_vs_outputs':[scalar(s) for s in reflection.outputSignature],'storage_reflection':[{'resource':scalar(r),'type':scalar(r.variableType),'members':members(r.variableType)} for r in reflection.readWriteResources+reflection.readOnlyResources],'vertices':verts,'indices':idx,'vertex_count':n,'world_equation_max_abs_error':worlderr,'clip_equation_max_abs_error':cliperr}
   out['matches'].append(target)
  if candidates:out['debug_messages']=[{'category':str(m.category),'severity':str(m.severity),'message':m.description} for m in ctrl.GetDebugMessages()]
 assert len(out['matches'])==1,[(m['event'],m['instance_slot']) for m in out['matches']]
 ctrl.Shutdown();cap.Shutdown();return out
try:
 report['before']=extract(os.path.join(base,'captures','gbuffer_frame6348.rdc'),1.94597)
 report['after']=extract(os.path.join(base,'captures','gbuffer_frame28131.rdc'),2.94597)
 b=report['before']['matches'][0];a=report['after']['matches'][0]
 cpu_path=os.environ.get('PICCOLO_GBUFFER_CPU',os.path.join(os.getcwd(),'gbuffer-cpu-instance-data.json'))
 cpu=json.load(open(cpu_path));cpu_mats=[x['model_matrix'] for x in cpu['instances']]
 report['checks']={'same_event':a['event']==b['event'],'same_pipeline':a['pipeline']==b['pipeline'],'same_vertex_shader':a['vertex_shader']==b['vertex_shader'],'same_instance_slot':a['instance_slot']==b['instance_slot'],'same_vbuffers':a['vbuffers']==b['vbuffers'],'same_ibuffer':a['ibuffer']==b['ibuffer'],'same_vertex_bytes':a['input_vertex_bytes_sha256']==b['input_vertex_bytes_sha256'],'same_index_bytes':a['index_bytes_sha256']==b['index_bytes_sha256'],'same_camera':a['perframe_matrix']==b['perframe_matrix'],'cpu_capture_matrix_max_error':max(abs(x-y) for cm,am in zip(cpu_mats,a['all_instance_matrices']) for x,y in zip(cm,am)),'world_before_max_error':b['world_equation_max_abs_error'],'world_after_max_error':a['world_equation_max_abs_error'],'clip_before_max_error':b['clip_equation_max_abs_error'],'clip_after_max_error':a['clip_equation_max_abs_error'],'world_delta_max_error':max(abs(av['world'][j]-bv['world'][j]-(1 if j==0 else 0)) for av,bv in zip(a['vertices'],b['vertices']) for j in range(3)),'other_instance_matrices_unchanged':all(am==bm for i,(am,bm) in enumerate(zip(a['all_instance_matrices'],b['all_instance_matrices'])) if i!=a['instance_slot'])}
except Exception:report['error']=traceback.format_exc()
with open(os.path.join(base,'gbuffer-replay-report.json'),'w') as f:json.dump(report,f,indent=2)
sys.exit(0)
