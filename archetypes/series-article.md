---
title: {{ .File.ContentBaseName | replaceRE "-" " " | title | printf "%q" }}
slug: "{{ .File.ContentBaseName }}"
date: "{{ .Date }}"
description: ""
draft: true
content_stage: "article"
type: "reading-series"
series: "填写系列名称"
primary_series: "replace-with-series-id"
series_role: "article"
series_order: 1
weight: 0
tags: []
---

<!-- 先填写真实的系列 ID 和顺序；若同一 ID 有多个索引，用 reading_index 指向唯一入口。 -->

## 这篇解决什么问题
