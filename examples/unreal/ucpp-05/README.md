# UCPP-5 自有数据交接样例

接入原TwinArena模块的两个文件；依赖Core、CoreUObject、Engine。不是新工程。目标UE5.8.2，核对Epic5.8 API，尚未UE编译运行。

场景放一个AUcppHandoffProbe。默认运行五个有限纯数据任务：正常、同目标旧/新两代、业务已结束、Actor已Destroy。Tick只消费已完成任务；正常结果6，新一代5，结束目标0。第二次独立运行设bStopImmediately=true，关闭后先等待所有任务完成，再丢弃结果，拒绝新请求。

不在后台捕获this/UObject/弱指针，不依赖Game Thread回调来完成worker。真实线程由submit/worker/apply日志与Trace核对；检查所有ensure/Error，不把末尾Display当通过。原始日志、patch/source_revision/目标配置/build_id/run_id待具备原工程后保存。
