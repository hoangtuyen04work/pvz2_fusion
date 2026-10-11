using System;
using UnityEngine;

/// <summary>
/// Cấu hình Firebase REST API.
/// Người dùng chỉ cần nhập API Key và Realtime Database URL vào đây hoặc sửa trực tiếp qua Inspector/Code.
/// </summary>
public static class FirebaseConfig
{
    // Web API Key lấy từ Firebase Console -> Project Settings -> General -> Web API Key
    public static string ApiKey = "AIzaSyCuMnqStK92Ic1ffY28_V6aYn4VxBnxC94";

    // URL Realtime Database của Firebase
    public static string DatabaseUrl = "https://pvz2-5c9ed-default-rtdb.asia-southeast1.firebasedatabase.app";
    public static string ProjectId = "pvz2-5c9ed";

    // Kiểm tra cấu hình hợp lệ
    public static bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && !ApiKey.Contains("YOUR_WEB_API_KEY");
}
