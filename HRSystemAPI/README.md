# 廣宇科技 HR System API

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![API](https://img.shields.io/badge/API-REST-green.svg)](https://restfulapi.net/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-brightgreen.svg)](https://swagger.io/)


## 目錄

- [功能特色](#功能特色)
- [技術架構](#技術架構)
- [快速開始](#快速開始)
- [API 端點](#api-端點)
- [環境設定](#環境設定)
- [部署指南](#部署指南)
- [開發指南](#開發指南)
- [許可證](#許可證)

## 功能特色

### 帳號整合
- Keycloak OIDC 登入整合
- Access Token 和 Refresh Token 支援
- 使用者資訊查詢與驗證

### 基本資料管理
- 員工基本資訊查詢（工號、姓名、部門、職稱）
- 組織架構資訊
- 員工狀態管理

### 考勤查詢系統
- 個人出勤記錄查詢
- 指定日期打卡記錄
- 上下班刷卡時間追蹤
- 異常狀態檢測與報告

### 請假剩餘天數
- 各類假別剩餘天數查詢（特休、事假、病假、補休假）
- 周年制計算支援
- 天數與小時數自動轉換

### 未來功能（開發中）
-  薪資查詢系統
-  教育訓練時數管理
-  電子表單申請與簽核流程

## 技術架構

- **框架**: ASP.NET Core 8.0
- **API 文件**: Swagger/OpenAPI 3.0
- **資料存取**: Dapper + Entity Framework Core
- **資料庫**: SQL Server
- **身份驗證**: Keycloak OIDC
- **架構模式**: Repository Pattern + Service Layer

