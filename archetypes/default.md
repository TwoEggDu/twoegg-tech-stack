---
title: {{ .File.ContentBaseName | replaceRE "-" " " | title | printf "%q" }}
slug: "{{ .File.ContentBaseName }}"
date: "{{ .Date }}"
description: ""
draft: true
content_stage: "article"
weight: 0
featured: false
tags: []
---

## 这篇解决什么问题

<!-- 先写使用场景与问题，再展开判断依据、实现和验证。普通文章自动使用阅读样式。 -->
