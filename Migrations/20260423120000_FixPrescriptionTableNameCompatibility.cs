using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyeClinicApp.Migrations
{
    public partial class FixPrescriptionTableNameCompatibility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Prescriptions]', N'U') IS NULL
                BEGIN
                    IF OBJECT_ID(N'[dbo].[Prescription]', N'U') IS NOT NULL
                    BEGIN
                        EXEC sp_rename N'[dbo].[Prescription]', N'Prescriptions';
                    END
                    ELSE
                    BEGIN
                        CREATE TABLE [dbo].[Prescriptions](
                            [Id] [int] IDENTITY(1,1) NOT NULL,
                            [AppointmentId] [int] NOT NULL,
                            [DoctorId] [nvarchar](450) NOT NULL,
                            [Notes] [nvarchar](2000) NULL,
                            [FilePath] [nvarchar](500) NOT NULL,
                            [FileContentType] [nvarchar](50) NULL,
                            [RightEyeSph] [nvarchar](120) NULL,
                            [RightEyeCyl] [nvarchar](120) NULL,
                            [RightEyeAxis] [nvarchar](120) NULL,
                            [LeftEyeSph] [nvarchar](120) NULL,
                            [LeftEyeCyl] [nvarchar](120) NULL,
                            [LeftEyeAxis] [nvarchar](120) NULL,
                            [CreatedAt] [datetime2] NOT NULL,
                            CONSTRAINT [PK_Prescriptions] PRIMARY KEY CLUSTERED ([Id] ASC),
                            CONSTRAINT [FK_Prescriptions_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [dbo].[Appointments]([Id]) ON DELETE CASCADE,
                            CONSTRAINT [FK_Prescriptions_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION
                        );
                    END
                END

                IF OBJECT_ID(N'[dbo].[Prescriptions]', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prescriptions_AppointmentId' AND object_id = OBJECT_ID(N'[dbo].[Prescriptions]'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_Prescriptions_AppointmentId] ON [dbo].[Prescriptions]([AppointmentId]);
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Prescriptions_DoctorId' AND object_id = OBJECT_ID(N'[dbo].[Prescriptions]'))
                    BEGIN
                        CREATE INDEX [IX_Prescriptions_DoctorId] ON [dbo].[Prescriptions]([DoctorId]);
                    END;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Prescription]', N'U') IS NULL
                   AND OBJECT_ID(N'[dbo].[Prescriptions]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Prescriptions]', N'Prescription';
                END
                """);
        }
    }
}
