---
title: {{ .File.ContentBaseName | replaceRE "-" " " | title | printf "%q" }}
slug: "{{ .File.ContentBaseName }}"
date: "{{ .Date }}"
description: ""
draft: true
type: "reading-series"
series: "填写系列名称"
series_id: "replace-with-series-id"
series_role: "index"
series_order: 0
series_title: "填写系列名称"
series_entry: true
topic_key: "delivery"
reading_title: "填写系列名称"
reading_intro: ""
reading_number_by_position: true
reading_groups:
  - title: "阅读顺序"
    intro: ""
    first: 1
    last: 100
weight: 0
tags: []
---

<!-- 填写系列 ID、主题和目录范围。topic_key 可选 delivery / gameplay / frameworks / engine / performance。 -->

## 系列目录 {#chapters}

{{< reading-series-chapters >}}
