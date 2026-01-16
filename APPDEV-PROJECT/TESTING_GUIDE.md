# Worker Registration & Profile Creation - Testing Guide

## ? FIXED ISSUES

1. **Removed orphaned DELETE statement** - No more data loss
2. **Fixed duplicate SaveChangesAsync()** - Prevents double-save errors
3. **Added missing Worker table columns** - Latitude, Longitude, CompletedJobs, AverageRating
4. **Added default values** - String properties default to empty string (never null)
5. **Improved error messages** - Shows inner exception details for debugging

## ?? STEP-BY-STEP TESTING

### Step 1: Database Migration
```powershell
# In Visual Studio Package Manager Console:
Update-Database
```
? Verify all migrations apply successfully (should see multiple migration names applied)

### Step 2: Register as Worker
1. Go to http://localhost:port/Home/LandingPage
2. Click "Worker" button
3. Click "Register" link
4. Fill in registration form:
   - Email: `testworker@example.com`
   - Password: `TestPass123!`
   - Confirm Password: `TestPass123!`
   - User Type: Select "Worker"
5. Click "Register" button
6. ? Should redirect to InfoPage_W (Fill Up Information)

### Step 3: Fill Worker Information Form
1. **Personal Details:**
   - First Name: `Juan`
   - Middle Name: `M`
   - Last Name: `Dela Cruz`
   - Date of Birth: `1990-01-15`
   - Sex: `Male`

2. **Contact Information:**
   - Email: `juan.delacruz@example.com`
   - Phone Number: `09123456789`
   - Address: `123 Sampaguita St., San Juan City, Metro Manila, Philippines`
   - Barangay: `San Juan Greenhills`

3. **Work Background:**
   - Skill: `Plumbing`
   - Years of Experience: `5`
   - Accomplishments: `Licensed Plumber with 5 years experience`

4. Click "Submit" button
5. ? **Expected:** Successfully redirects to LoginPage with confirmation

### Step 4: Verify Data in Database
Run this SQL query in SQL Server Management Studio:
```sql
SELECT * FROM Workers WHERE FName = 'Juan';
SELECT * FROM Users WHERE Email = 'testworker@example.com';
```
? Verify:
- Worker record exists with all fields populated
- UserId matches the User record
- Latitude and Longitude have values
- CompletedJobs = 0
- AverageRating = 0.0

### Step 5: Login and View Profile
1. Go to http://localhost:port/Account/LoginPage
2. Login with:
   - Email: `testworker@example.com`
   - Password: `TestPass123!`
   - User Type: `Worker`
3. ? Should successfully login and redirect to Profile_W

### Step 6: View Profile Information
1. On Profile page, verify all information displays correctly:
   - Name: Juan M. Dela Cruz
   - Skill: Plumbing
   - Experience: 5+ years
   - Location: San Juan Greenhills
   - All contact information

## ?? IF ERRORS OCCUR

### Error: "User session expired"
- **Cause:** Session was cleared or registration failed silently
- **Fix:** Register again, check browser cookies are enabled

### Error: "Foreign key constraint failed"
- **Cause:** User doesn't exist in Users table
- **Fix:** Verify registration created User record
  ```sql
  SELECT * FROM Users WHERE Email = 'testworker@example.com';
  ```

### Error: "Cannot insert NULL into [column]"
- **Cause:** Missing required field
- **Fix:** Check all form fields are filled; verify model binding

### Error: "Invalid column name"
- **Cause:** Migrations didn't apply properly
- **Fix:** Run `Update-Database` again
  ```powershell
  Update-Database -Verbose
  ```

## ?? QUICK CHECKLIST

- [ ] Database migrated successfully
- [ ] Worker registration form accessible
- [ ] Can fill and submit worker info form
- [ ] No errors in submission
- [ ] Redirects to login page
- [ ] Can login with worker credentials
- [ ] Profile page shows all information
- [ ] Worker record exists in database
- [ ] User record exists in database
- [ ] Foreign key relationship intact

## ?? SUCCESS INDICATORS

? All of the above completed without errors = **System is working properly**

If any step fails, check the error message carefully - it now contains inner exception details that will help diagnose the issue.
