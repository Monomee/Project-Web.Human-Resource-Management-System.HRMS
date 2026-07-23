# Attendance Module Design

## Overview

The Attendance module is designed for an office-based Human Resource Management System (HRMS). Employees are allowed to check in once and check out once per working day. Attendance records store only original attendance data, while working hours, lateness, overtime, and attendance status are calculated dynamically.

Each position has a **default working shift**. If an employee is temporarily assigned to a different shift, the system uses **ShiftAssignment** to override the default shift for a specified period.

---

# Functional Requirements

## Employee

- Check in once per working day.
- Check out once per working day.
- View personal attendance history.

## HR / Administrator

- View attendance records.
- Edit attendance records.
- Manage shifts.
- Assign temporary shifts to employees.

---

# Database Design

## Shift

```text
Shift
-----
Id
Name
StartTime
EndTime
BreakStart
BreakEnd
LateToleranceMinute
EarlyCheckInMinute
LateCheckOutMinute
IsActive
```

---

## Position

Stores job positions and their default working shift.

```text
Positions
--------
Id
Name
DefaultShiftId
```

---

## Employee

```text
Users
--------
Id
DepartmentId
PositionId
...
```

---

## ShiftAssignment

Stores temporary shift assignments that override the default shift defined by the employee's position.

```text
ShiftAssignment
---------------
Id
EmployeeId
ShiftId
StartDate
EndDate
AssignedBy
CreatedAt
```

Rules

- A shift assignment is optional.
- If an active shift assignment exists, it takes precedence over the default shift.
- Otherwise, the system uses `Position.DefaultShiftId`.

---

## Attendance

Stores one attendance record per employee per working day.

```text
Attendance
----------
Id
EmployeeId
AttendanceDate
CheckInTime
CheckOutTime
```

Database Constraint

```sql
UNIQUE(EmployeeId, AttendanceDate)
```

---

# Business Rules

## Determine Working Shift

When processing attendance, the system determines the working shift using the following priority:

1. Retrieve the employee's active `ShiftAssignment`.
2. If one exists, use its `ShiftId`.
3. Otherwise, use the employee's `Position.DefaultShiftId`.

---

## Check In

1. Employee must be authenticated.
2. Verify that no attendance record exists for the current date.
3. Determine the employee's working shift.
4. Create the attendance record.
5. Store:
   - AttendanceDate
   - CheckInTime
---

## Check Out

1. Retrieve today's attendance record.
2. Reject the request if the employee has not checked in.
3. Reject the request if `CheckOutTime` already exists.
4. Update `CheckOutTime`.

---

# Attendance Calculation

Attendance information is calculated dynamically.

The AttendanceCalculator uses:

- CheckInTime
- CheckOutTime
- Shift.StartTime
- Shift.EndTime
- BreakStart
- BreakEnd

to calculate:

- Working Minutes
- Late Minutes
- Early Leave Minutes
- Overtime Minutes
- Attendance Status

These values are not stored in the Attendance table.

---

# Attendance Service

Suggested methods

- CheckInAsync()
- CheckOutAsync()
- GetTodayAttendanceAsync()
- GetAttendanceHistoryAsync()
- UpdateAttendanceAsync()

---
