# HR System API - PowerShell Test Script

$baseUrl = "http://localhost:5147"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "HR System API - SSO Function Test" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check Account Status
Write-Host "Test 1: Check Account Status" -ForegroundColor Yellow
Write-Host "--------------------------------------"
$uid = "E001"
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/Auth/check-status?uid=$uid" -Method Get
    Write-Host "Success" -ForegroundColor Green
    Write-Host "  Employee No: $($response.uid)" -ForegroundColor White
    Write-Host "  Is Active: $($response.isActive)" -ForegroundColor White
    Write-Host "  Status Code: $($response.status)" -ForegroundColor White
    Write-Host "  Status Name: $($response.statusName)" -ForegroundColor White
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Login and Get Token ID
Write-Host "Test 2: Login and Get Token ID" -ForegroundColor Yellow
Write-Host "--------------------------------------"
$loginBody = @{
    email = "test@company.com"
    password = "password123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "Login Success" -ForegroundColor Green
    Write-Host "  Token ID: $($loginResponse.tokenId)" -ForegroundColor White
    Write-Host "  Employee No (uid): $($loginResponse.uid)" -ForegroundColor White
    Write-Host "  Employee Name: $($loginResponse.employeeName)" -ForegroundColor White
    Write-Host "  Email: $($loginResponse.email)" -ForegroundColor White
    Write-Host "  Is Active: $($loginResponse.isActive)" -ForegroundColor White
    Write-Host "  Status: $($loginResponse.status)" -ForegroundColor White
    
    $tokenId = $loginResponse.tokenId
    
    # Test 3: Validate Token ID
    Write-Host ""
    Write-Host "Test 3: Validate Token ID" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    $validateBody = @{
        tokenId = $tokenId
    } | ConvertTo-Json
    
    try {
        $validateResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/validate-token" -Method Post -Body $validateBody -ContentType "application/json"
        Write-Host "Token Validation Success" -ForegroundColor Green
        Write-Host "  Token Is Valid: $($validateResponse.isValid)" -ForegroundColor White
        Write-Host "  Message: $($validateResponse.message)" -ForegroundColor White
    } catch {
        Write-Host "Token Validation Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Login Failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # If login fails, test with invalid token
    Write-Host ""
    Write-Host "Test 3b: Validate Invalid Token ID" -ForegroundColor Yellow
    Write-Host "--------------------------------------"
    
    $invalidTokenBody = @{
        tokenId = "0000000000"
    } | ConvertTo-Json
    
    try {
        $validateResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/validate-token" -Method Post -Body $invalidTokenBody -ContentType "application/json"
        Write-Host "  Token Is Valid: $($validateResponse.isValid)" -ForegroundColor White
        Write-Host "  Message: $($validateResponse.message)" -ForegroundColor White
    } catch {
        Write-Host "Validation Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# Test 4: Get User Info
Write-Host "Test 4: Get User Info" -ForegroundColor Yellow
Write-Host "--------------------------------------"
$email = "test@company.com"
try {
    $userInfo = Invoke-RestMethod -Uri "$baseUrl/api/Auth/userinfo?email=$email" -Method Get
    Write-Host "Success" -ForegroundColor Green
    Write-Host "  Employee No (uid): $($userInfo.uid)" -ForegroundColor White
    Write-Host "  Employee Name: $($userInfo.employeeName)" -ForegroundColor White
    Write-Host "  Email: $($userInfo.email)" -ForegroundColor White
    Write-Host "  Company: $($userInfo.companyName)" -ForegroundColor White
    Write-Host "  Department: $($userInfo.departmentName)" -ForegroundColor White
    Write-Host "  Job Title: $($userInfo.jobTitle)" -ForegroundColor White
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: FTP Connection Test
Write-Host "Test 5: FTP Connection Test" -ForegroundColor Yellow
Write-Host "--------------------------------------"
try {
    $ftpTest = Invoke-RestMethod -Uri "$baseUrl/api/OutingForm/test-ftp" -Method Get
    Write-Host "FTP Test Complete" -ForegroundColor Green
    Write-Host "  Connection Success: $($ftpTest.success)" -ForegroundColor White
    Write-Host "  Message: $($ftpTest.message)" -ForegroundColor White
} catch {
    Write-Host "FTP Test Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test Summary
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Test Complete" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Important Notes:" -ForegroundColor Yellow
Write-Host "1. Please confirm test employee data exists in database" -ForegroundColor White
Write-Host "2. Token ID format: nn + ssssss + nn (2-digit random + 6-digit time + 2-digit random)" -ForegroundColor White
Write-Host "3. Employee status: W=Active, S=Suspended, X=Permanently Disabled" -ForegroundColor White
Write-Host "4. Only accounts with uwork='W' can login" -ForegroundColor White

