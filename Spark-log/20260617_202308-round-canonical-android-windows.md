# Spark Log
- 模式: Spark
- 对话时间: $(date '+%Y-%m-%d %H:%M:%S %z')
- 目标: 只读 Apple canonical 参考，全量迁移 canonical 内核到 Android/Windows


- [$(date +%F_%T)] 开始本轮：扫描Apple canonical kernel协议并对齐Android/Windows。

- [$(date +  - 已完成：读取 Apple canonical 内核基准定义（KernelProtocols / TransferStateMachine / SyncStatusTruth）并开始迁移。
  - Android: 已替换 CanonicalCanonicalCoreCompatTypes 的协议原子类型（Node/Role/Identity/Version/Time/Sequence/ObjectID/Domain/Mode/PortableBoundary）；已同步旧字段引用为新模式 oldKernel、iPhone。
  - Windows: 在 CanonicalCoreCompatTypes.cs 补齐协议原子与模式枚举（NodeID/NodeRole/NodeIdentity/ProtocolVersion/LogicalTime/Sequence/Domain/Mode/Boundary）。
  - 未完成：未开始同步 CanonicalSyncStatusTruthProtocol 的完整语义逻辑与 TransferStateMachine 深度差异校验。
- [2026-06-17_20:25:18] 本轮进度：
  - 已完成：读取 Apple canonical 内核基准定义（KernelProtocols / TransferStateMachine / SyncStatusTruth）并开始迁移。
  - Android: 已替换 CanonicalCanonicalCoreCompatTypes 的协议原子类型（Node/Role/Identity/Version/Time/Sequence/ObjectID/Domain/Mode/PortableBoundary）；已同步旧字段引用为新模式 oldKernel、iPhone。
  - Windows: 在 CanonicalCoreCompatTypes.cs 补齐协议原子与模式枚举（NodeID/NodeRole/NodeIdentity/ProtocolVersion/LogicalTime/Sequence/Domain/Mode/Boundary）。
  - 未完成：未开始同步 CanonicalSyncStatusTruthProtocol 的完整语义逻辑与 TransferStateMachine 深度差异校验。
- [2026-06-17_20:25:53] 本轮补充：Windows 新增 CanonicalTransferStateMachine.cs（TransferJob/Kind/Direction/Phase/Failure/Projection/StateMachineCompat），采用 Apple TransferStateMachine 的字段映射语义。
- [2026-06-17_20:25:58] 本轮补齐：Android 增加 CanonicalStatusFactID 名称映射（typealias 兼容）；Windows 全局别名增加 CanonicalStatusFactID = string。
