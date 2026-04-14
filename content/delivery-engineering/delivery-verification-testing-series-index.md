---
date: "2026-04-14"
title: "验证与测试系列索引｜功能、性能、稳定性、兼容性四层验证体系"
description: "给交付工程专栏 V13 补一个系统入口：从验证体系全局视角出发，覆盖验证门设计、功能验证、性能测试、稳定性测试和兼容性测试六篇文章。"
slug: "delivery-verification-testing-series-index"
weight: 1200
featured: false
tags:
  - "Delivery Engineering"
  - "Verification"
  - "Testing"
  - "Quality"
  - "Index"
series: "验证与测试"
series_id: "delivery-verification-testing"
series_role: "index"
series_order: 0
series_nav_order: 13
series_title: "验证与测试"
series_entry: true
series_audience:
  - "QA/测试负责人"
  - "客户端/引擎开发"
  - "技术负责人/主程"
series_level: "进阶"
series_best_for: "当你想搞清楚验证和测试的区别、怎么设计验证门、四层验证体系怎么落地"
series_summary: "把验证从'手工跑一遍'升级到工程化体系：验证门分级拦截、功能回归自动化、性能预算基线检测、稳定性长时间跑测、兼容性设备矩阵覆盖。"
series_intro: "V12 讲了服务端版本管理和热更新。V13 回到交付质量本身——产品能不能发、发了会不会出事。验证不是测试部门的事，而是整个交付链路的质量闸门。"
delivery_layer: "principle"
delivery_volume: "V13"
delivery_reading_lines:
  - "L1"
  - "L2"
  - "L4"
---

## 系列导读

测试是手段，验证是目的。测试回答"有没有 bug"，验证回答"这个版本能不能发"。很多团队把测试和验证混为一谈——测试通过了就发，测试没通过就修。但"测试通过"不等于"可以发布"，中间还差验证门的判定。

V13 回答六个核心问题：验证体系的全局视角是什么、验证门怎么设计、功能验证怎么做回归和自动化、性能测试怎么检测退化、稳定性测试怎么发现 Crash 和内存泄漏、兼容性测试怎么覆盖碎片化的设备。

## 文章列表

| 序号 | 标题 | 核心问题 |
|------|------|---------|
| 01 | [验证体系全局视角]({{< relref "delivery-engineering/delivery-verification-testing-01-overview.md" >}}) | 验证和测试的区别是什么？四层验证模型怎么组织？ |
| 02 | [验证门设计]({{< relref "delivery-engineering/delivery-verification-testing-02-gates.md" >}}) | 什么阶段拦什么问题？门的判定标准怎么定？ |
| 03 | [功能验证]({{< relref "delivery-engineering/delivery-verification-testing-03-functional.md" >}}) | 冒烟、回归、自动化——功能验证的优先级怎么排？ |
| 04 | [性能测试]({{< relref "delivery-engineering/delivery-verification-testing-04-performance.md" >}}) | 性能预算基线怎么定？性能退化怎么自动检测？ |
| 05 | [稳定性测试]({{< relref "delivery-engineering/delivery-verification-testing-05-stability.md" >}}) | Crash、ANR、OOM、弱网——稳定性怎么量化验证？ |
| 06 | [兼容性测试]({{< relref "delivery-engineering/delivery-verification-testing-06-compatibility.md" >}}) | 设备矩阵怎么选？云真机怎么用？GPU 兼容怎么测？ |

## 与其他系列的关系

V12 讲了服务端版本协调和热更新，V13 聚焦交付前的验证环节——用四层验证体系确保每个版本都经过功能、性能、稳定性和兼容性的检验。V14 将讲性能预算与监控，与本系列的性能测试形成闭环。
