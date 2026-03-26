---
date: "2026-03-26"
title: "数据结构与算法 18｜环形缓冲区与双缓冲：无锁队列与渲染同步"
description: "环形缓冲区（Ring Buffer）是固定大小、无需内存分配的 FIFO 队列，是网络消息处理、音频流、输入录制的标准实现。双缓冲是渲染和状态更新的经典同步模式。这篇讲清楚它们的原理、实现和游戏里的具体用法。"
slug: "ds-18-ring-buffer"
weight: 775
tags:
  - 软件工程
  - 数据结构
  - 环形缓冲区
  - 双缓冲
  - 并发
  - 游戏架构
series: "数据结构与算法"
---

> 网络游戏每帧收到大量玩家输入包，不能立即处理（游戏逻辑还在跑），也不能丢弃，需要一个先进先出的缓冲。普通队列每次入队出队可能触发 GC，环形缓冲区预分配固定内存，完全无 GC，天然适合实时游戏。

---

## 环形缓冲区（Ring Buffer / Circular Buffer）

用一个固定大小的数组，加上 head（读指针）和 tail（写指针）实现 FIFO 队列。当指针到达数组末尾时，绕回开头——形成"环形"。

```
初始状态：
  [_, _, _, _, _]  head=0, tail=0, count=0

写入 A, B, C：
  [A, B, C, _, _]  head=0, tail=3, count=3

读取（得到 A）：
  [_, B, C, _, _]  head=1, tail=3, count=2

写入 D, E, F（F 超出后绕回）：
  [F, B, C, D, E]  head=1, tail=1, count=5（满）
```

---

## 实现

```csharp
public class RingBuffer<T>
{
    private readonly T[] buffer;
    private int head;   // 下一个读取位置
    private int tail;   // 下一个写入位置
    private int count;

    public int  Capacity => buffer.Length;
    public int  Count    => count;
    public bool IsEmpty  => count == 0;
    public bool IsFull   => count == buffer.Length;

    public RingBuffer(int capacity)
    {
        buffer = new T[capacity];
    }

    // 入队（写入）
    public bool TryEnqueue(T item)
    {
        if (IsFull) return false;
        buffer[tail] = item;
        tail = (tail + 1) % buffer.Length;
        count++;
        return true;
    }

    // 出队（读取）
    public bool TryDequeue(out T item)
    {
        if (IsEmpty) { item = default; return false; }
        item = buffer[head];
        buffer[head] = default;  // 释放引用，帮助 GC
        head = (head + 1) % buffer.Length;
        count--;
        return true;
    }

    // 只查看不取出
    public bool TryPeek(out T item)
    {
        if (IsEmpty) { item = default; return false; }
        item = buffer[head];
        return true;
    }

    // 查看特定偏移量的元素（不修改指针）
    public T this[int offset] => buffer[(head + offset) % buffer.Length];
}
```

**关键**：`(index + 1) % capacity` 实现绕回，完全无内存分配。

---

## 游戏场景一：网络消息队列

```csharp
public class NetworkMessageQueue
{
    // 预分配固定大小缓冲区，避免网络高峰期 GC
    private RingBuffer<NetworkMessage> incoming = new(1024);
    private RingBuffer<NetworkMessage> outgoing = new(512);

    // 网络线程写入（收到消息）
    public void OnReceive(NetworkMessage msg)
    {
        if (!incoming.TryEnqueue(msg))
            Debug.LogWarning("消息队列已满，丢弃消息！");
    }

    // 游戏主线程每帧读取并处理
    public void ProcessAll()
    {
        while (incoming.TryDequeue(out var msg))
            DispatchMessage(msg);
    }

    private void DispatchMessage(NetworkMessage msg) { /* 处理消息 */ }
}
```

---

## 游戏场景二：输入录制与回放

```csharp
// 录制最近 N 帧的输入（用于即时重播、时间倒流机制）
public class InputRecorder
{
    private RingBuffer<InputFrame> history = new(300);  // 约 5 秒（60fps）

    [System.Serializable]
    public struct InputFrame
    {
        public int      frameIndex;
        public Vector2  moveInput;
        public bool     jumpPressed;
        public bool     attackPressed;
    }

    void Update()
    {
        var frame = new InputFrame
        {
            frameIndex   = Time.frameCount,
            moveInput    = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
            jumpPressed  = Input.GetButtonDown("Jump"),
            attackPressed = Input.GetButtonDown("Fire1")
        };

        // 环形缓冲区满了会自动覆盖最旧的记录
        if (history.IsFull) history.TryDequeue(out _);
        history.TryEnqueue(frame);
    }

    // 获取 N 帧前的输入（用于时间倒流）
    public InputFrame GetFrameAgo(int framesAgo)
    {
        int offset = history.Count - 1 - framesAgo;
        if (offset < 0) return default;
        return history[offset];
    }
}
```

---

## 游戏场景三：音频流缓冲

音频系统的生产者（解码线程）和消费者（音频硬件回调）需要解耦：

```csharp
public class AudioStreamBuffer
{
    // 存储 PCM 采样数据
    private RingBuffer<float> pcmBuffer = new(44100 * 2);  // 2 秒的 44.1kHz 音频

    // 解码线程：写入解码好的 PCM 数据
    public void WriteSamples(float[] samples)
    {
        foreach (var s in samples)
            if (!pcmBuffer.TryEnqueue(s))
                break;  // 缓冲区满，跳过（可能出现爆音，需要更大缓冲区）
    }

    // 音频回调（硬件线程）：读取 PCM 数据
    public void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (pcmBuffer.TryDequeue(out float sample))
                data[i] = sample;
            else
                data[i] = 0f;  // 缓冲区空，静音（可能出现卡顿）
        }
    }
}
```

---

## 单生产者单消费者的无锁版本

当生产者和消费者在不同线程，但保证"只有一个生产者，只有一个消费者"时，可以用无锁版本：

```csharp
public class LockFreeRingBuffer<T>
{
    private readonly T[] buffer;
    // 用 volatile 保证可见性（head 由消费者写，tail 由生产者写）
    private volatile int head = 0;
    private volatile int tail = 0;

    public LockFreeRingBuffer(int capacity)
    {
        // 容量必须是 2 的幂（用位运算代替取模）
        int pow2 = 1;
        while (pow2 < capacity) pow2 <<= 1;
        buffer = new T[pow2];
    }

    private int Mask => buffer.Length - 1;

    public bool TryEnqueue(T item)
    {
        int currentTail = tail;
        int nextTail    = (currentTail + 1) & Mask;
        if (nextTail == head) return false;  // 满了

        buffer[currentTail] = item;
        tail = nextTail;  // volatile 写，保证对消费者可见
        return true;
    }

    public bool TryDequeue(out T item)
    {
        int currentHead = head;
        if (currentHead == tail) { item = default; return false; }  // 空

        item = buffer[currentHead];
        buffer[currentHead] = default;
        head = (currentHead + 1) & Mask;  // volatile 写
        return true;
    }
}
```

**注意**：这个无锁版本只在"单生产者单消费者"时正确。多生产者或多消费者场景需要用 `Interlocked` 操作或 `System.Threading.Channels`。

---

## 双缓冲（Double Buffering）

双缓冲是另一种缓冲模式：**写入一个缓冲区的同时，从另一个缓冲区读取**，然后交换。

### 渲染双缓冲（GPU 的标配）

```
前缓冲（Front Buffer）：显示器正在显示的画面
后缓冲（Back Buffer）：GPU 正在渲染的画面

渲染完成后：交换前后缓冲（Swap / Present）
→ 玩家永远看到完整的帧，不会看到"渲染一半"的画面（撕裂）

VSync：等显示器扫描到底部后才交换（消除画面撕裂，但增加延迟）
Triple Buffering：三个缓冲区，减少等待时间
```

### 游戏逻辑双缓冲

并行更新场景状态时，防止"读到自己写的值"：

```csharp
// 细胞自动机（Game of Life）：
// 每个细胞的下一代状态取决于当前邻居，
// 必须所有细胞同时更新（不能用更新后的值计算其他细胞）

bool[,] currentState = new bool[width, height];
bool[,] nextState    = new bool[width, height];

void Update()
{
    // 读 currentState，写 nextState
    for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
        nextState[x, y] = CalculateNextState(currentState, x, y);

    // 交换
    (currentState, nextState) = (nextState, currentState);
}
```

```csharp
// 粒子系统：GPU 双缓冲
// Compute Shader 里，粒子位置的读缓冲和写缓冲每帧交换
// 避免写了一半粒子时，其他粒子已经读到更新后的碰撞数据
```

---

## 环形缓冲区 vs 普通队列

| | Queue\<T\> | RingBuffer\<T\> |
|---|---|---|
| 内存分配 | 动态（可能触发 GC） | 预分配，零 GC |
| 容量限制 | 无限（受堆内存限制） | 固定容量 |
| 满了怎么办 | 自动扩容 | 入队失败 / 覆盖旧数据 |
| 缓存友好 | 链表节点分散 | 连续数组，缓存友好 |
| 适用场景 | 一般用途 | 实时系统、网络、音频 |

---

## 小结

- **环形缓冲区**：固定大小数组 + 读写指针，O(1) 入队出队，零 GC
- **`% capacity`**：绕回操作；容量取 2 的幂时可改用位运算 `& (capacity-1)` 更快
- **单生产者单消费者**：两个线程只访问各自的指针（head / tail），`volatile` 保证可见性，无需锁
- **游戏应用**：网络消息队列、输入录制、音频流、日志缓冲
- **双缓冲**：渲染的标准同步方式（GPU 前后缓冲），也用于游戏逻辑的并行状态更新（细胞自动机、粒子系统）
