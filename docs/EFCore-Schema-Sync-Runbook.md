# EF Core Runtime Error Runbook: "Invalid column name"

## Symptoms
At runtime, EF Core queries fail with SQL Server errors like:

- `Invalid column name 'AssignedDoctorId'`
- `Invalid column name 'PaymentMethod'`
- `Invalid column name 'PaymentStatus'`
- `Invalid column name 'RazorpayOrderId'`
- `Invalid column name 'RazorpayPaymentId'`

## Root cause
The application model (`Appointment` entity + `ApplicationDbContext`) expects columns that do not yet exist in the target SQL Server database. This is a model/database schema drift issue.

In this repository, those fields are represented in:

- `Models/Appointment.cs`
- `Data/ApplicationDbContext.cs`
- migration `Migrations/20260411100000_AddAppointmentPaymentsRolesAndPrescriptions.cs`

If this migration is present in code but missing from `__EFMigrationsHistory` in production, runtime SELECT queries (for example during `ToListAsync`) will fail when SQL tries to project non-existent columns.

## Safe fix approach (production)
1. Verify you are pointing to the correct production DB connection string.
2. Verify migration status (`dotnet ef migrations list` and DB `__EFMigrationsHistory`).
3. Apply pending migration:

```bash
dotnet ef migrations add AddAppointmentPaymentColumnsAndDoctorAssignment
# only run add if migration does not already exist

dotnet ef database update
```

> Note: In this repository, migration `20260411100000_AddAppointmentPaymentsRolesAndPrescriptions` already contains the required `Appointments` columns and is the migration that should be applied if pending.

## Why this migration is safe
The new columns on `Appointments` are additive and production-safe:

- `AssignedDoctorId`: nullable
- `PaymentMethod`: non-null with default `'Clinic'`
- `PaymentStatus`: non-null with default `'Pending'`
- `RazorpayOrderId`: nullable
- `RazorpayPaymentId`: nullable

This preserves existing rows and avoids destructive operations.

## SQL fallback (manual DBA path)

```sql
IF COL_LENGTH('dbo.Appointments', 'AssignedDoctorId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [AssignedDoctorId] nvarchar(450) NULL;
END;

IF COL_LENGTH('dbo.Appointments', 'PaymentMethod') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [PaymentMethod] nvarchar(30) NOT NULL CONSTRAINT [DF_Appointments_PaymentMethod] DEFAULT('Clinic');
END;

IF COL_LENGTH('dbo.Appointments', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [PaymentStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_Appointments_PaymentStatus] DEFAULT('Pending');
END;

IF COL_LENGTH('dbo.Appointments', 'RazorpayOrderId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [RazorpayOrderId] nvarchar(120) NULL;
END;

IF COL_LENGTH('dbo.Appointments', 'RazorpayPaymentId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [RazorpayPaymentId] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Appointments_AssignedDoctorId'
      AND object_id = OBJECT_ID('dbo.Appointments')
)
BEGIN
    CREATE INDEX [IX_Appointments_AssignedDoctorId]
        ON [dbo].[Appointments]([AssignedDoctorId]);
END;
```

Optional FK once app role data is verified:

```sql
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Appointments_AspNetUsers_AssignedDoctorId'
)
BEGIN
    ALTER TABLE [dbo].[Appointments]
        ADD CONSTRAINT [FK_Appointments_AspNetUsers_AssignedDoctorId]
        FOREIGN KEY ([AssignedDoctorId]) REFERENCES [dbo].[AspNetUsers]([Id])
        ON DELETE SET NULL;
END;
```

## Validation checklist
- Confirm missing columns now exist in `dbo.Appointments`.
- Confirm latest migration row exists in `__EFMigrationsHistory`.
- Re-run failing endpoint and ensure `ToListAsync` succeeds.
- Validate read/write appointment flows and payment status updates.

## Prevention
- Always create migration immediately after model changes.
- Always apply migrations in each environment (Dev/Staging/Prod).
- Keep one deployment step that validates `dotnet ef migrations list` vs DB history.
- Avoid hotfixing entities without matching migration rollout.
