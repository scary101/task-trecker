using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace steptreck.API.Models;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppAuditLog> AppAuditLogs { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ConfirmationCode> ConfirmationCodes { get; set; }

    public virtual DbSet<EmployeeWorkStatus> EmployeeWorkStatuses { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<LoginChallenge> LoginChallenges { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberScore> MemberScores { get; set; }

    public virtual DbSet<MemberScoreLog> MemberScoreLogs { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OrgRole> OrgRoles { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<PasswordReset> PasswordResets { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Plan> Plans { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectCalendarEvent> ProjectCalendarEvents { get; set; }

    public virtual DbSet<ProjectTask> ProjectTasks { get; set; }

    public virtual DbSet<ProjectTaskChecklistItem> ProjectTaskChecklistItems { get; set; }

    public virtual DbSet<ProjectTeam> ProjectTeams { get; set; }

    public virtual DbSet<ProjectTeamMember> ProjectTeamMembers { get; set; }

    public virtual DbSet<PushToken> PushTokens { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SubscriptionItem> SubscriptionItems { get; set; }

    public virtual DbSet<SubscriptionStatus> SubscriptionStatuses { get; set; }

    public virtual DbSet<TaskDeadlineNotification> TaskDeadlineNotifications { get; set; }

    public virtual DbSet<TaskPriority> TaskPriorities { get; set; }

    public virtual DbSet<TeamChatMessage> TeamChatMessages { get; set; }

    public virtual DbSet<TeamChatMessageReaction> TeamChatMessageReactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLock> UserLocks { get; set; }

    public virtual DbSet<WorkSession> WorkSessions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=62.109.2.222;Port=5432;Database=steptreck_db;Username=super;Password=super");

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void NormalizeDateTimesToUtc()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is not DateTime value)
                    continue;

                var clrType = property.Metadata.ClrType;
                if (clrType != typeof(DateTime) && clrType != typeof(DateTime?))
                    continue;

                if (value == default &&
                    property.Metadata.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never)
                {
                    continue;
                }

                property.CurrentValue = EnsureUtc(value);
            }
        }
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("calendar_event_type", new[] { "TaskCreated", "TaskDeadline", "TaskCompleted", "Meeting", "ImportantDate", "Custom" })
            .HasPostgresExtension("citext");

        modelBuilder.Entity<AppAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("app_audit_logs_pkey");

            entity.ToTable("app_audit_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.ActorMemberId).HasColumnName("actor_member_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityName).HasColumnName("entity_name");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(64)
                .HasColumnName("ip_address");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasColumnName("new_values");
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasColumnName("old_values");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.TargetMemberId).HasColumnName("target_member_id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attachments_pkey");

            entity.ToTable("attachments");

            entity.HasIndex(e => e.ProjectId, "ix_attachments_project");

            entity.HasIndex(e => e.TaskId, "ix_attachments_task");

            entity.HasIndex(e => e.TeamId, "ix_attachments_team");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ContentType).HasColumnName("content_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FileName).HasColumnName("file_name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.StorageKey).HasColumnName("storage_key");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("attachments_organization_id_fkey");

            entity.HasOne(d => d.Project).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachments_project_id_fkey");

            entity.HasOne(d => d.Task).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_attachments_task");

            entity.HasOne(d => d.Team).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachments_team_id_fkey");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("attachments_uploaded_by_user_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasColumnName("new_values");
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasColumnName("old_values");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName).HasColumnName("table_name");
        });

        modelBuilder.Entity<ConfirmationCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("confirmation_codes_pkey");

            entity.ToTable("confirmation_codes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(255)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.ConfirmationCodes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_confirmation_codes_user");
        });

        modelBuilder.Entity<EmployeeWorkStatus>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("employee_work_statuses_pkey");

            entity.ToTable("employee_work_statuses");

            entity.HasIndex(e => new { e.OrgId, e.CurrentStatus }, "ix_employee_status_org_status");

            entity.HasIndex(e => e.OrgId, "ix_employee_work_statuses_org_id");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentSessionId).HasColumnName("current_session_id");
            entity.Property(e => e.CurrentStatus).HasColumnName("current_status");
            entity.Property(e => e.OrgId).HasColumnName("org_id");
            entity.Property(e => e.StatusStartedAtUtc).HasColumnName("status_started_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne(d => d.CurrentSession).WithMany(p => p.EmployeeWorkStatuses)
                .HasForeignKey(d => d.CurrentSessionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_employee_work_statuses_current_session_id");

            entity.HasOne(d => d.Org).WithMany(p => p.EmployeeWorkStatuses)
                .HasForeignKey(d => d.OrgId)
                .HasConstraintName("fk_employee_work_statuses_org");

            entity.HasOne(d => d.User).WithOne(p => p.EmployeeWorkStatus)
                .HasForeignKey<EmployeeWorkStatus>(d => d.UserId)
                .HasConstraintName("fk_employee_work_statuses_user_id");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invitations_pkey");

            entity.ToTable("invitations");

            entity.HasIndex(e => new { e.OrganizationId, e.CorporateEmail }, "ix_invitations_org_corp_email");

            entity.HasIndex(e => e.RoleId, "ix_invitations_role_id");

            entity.HasIndex(e => e.TokenHash, "ux_invitations_token_hash").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CorporateEmail)
                .HasColumnType("citext")
                .HasColumnName("corporate_email");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");

            entity.HasOne(d => d.Organization).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("invitations_organization_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("invitations_role_id_fkey");
        });

        modelBuilder.Entity<LoginChallenge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("login_challenges_pkey");

            entity.ToTable("login_challenges");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("status");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.LoginChallenges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_login_challenges_user");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("members_pkey");

            entity.ToTable("members");

            entity.HasIndex(e => e.OrganizationId, "ix_members_organization_id");

            entity.HasIndex(e => e.RoleId, "ix_members_role_id");

            entity.HasIndex(e => e.Username, "ix_members_username")
                .IsUnique()
                .HasFilter("(username IS NOT NULL)");

            entity.HasIndex(e => new { e.OrganizationId, e.UserId }, "ux_members_org_user").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Patronymic).HasColumnName("patronymic");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Surname).HasColumnName("surname");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Username)
                .HasMaxLength(64)
                .HasColumnName("username");

            entity.HasOne(d => d.Organization).WithMany(p => p.Members)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("members_organization_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Members)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("members_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Members)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("members_user_id_fkey");
        });

        modelBuilder.Entity<MemberScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_scores_pkey");

            entity.ToTable("member_scores");

            entity.HasIndex(e => e.Trust, "ix_member_scores_trust").IsDescending();

            entity.HasIndex(e => e.MemberId, "ux_member_scores_member_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompletedCount)
                .HasDefaultValue(0)
                .HasColumnName("completed_count");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.MissedCount)
                .HasDefaultValue(0)
                .HasColumnName("missed_count");
            entity.Property(e => e.TotalAssignedCount)
                .HasDefaultValue(0)
                .HasColumnName("total_assigned_count");
            entity.Property(e => e.Trust)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("100")
                .HasColumnName("trust");
            entity.Property(e => e.UpdatedAtUtc)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at_utc");

            entity.HasOne(d => d.Member).WithOne(p => p.MemberScore)
                .HasForeignKey<MemberScore>(d => d.MemberId)
                .HasConstraintName("fk_member_scores_member");
        });

        modelBuilder.Entity<MemberScoreLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("member_score_logs_pkey");

            entity.ToTable("member_score_logs");

            entity.HasIndex(e => new { e.MemberId, e.CreatedAtUtc }, "ix_member_score_logs_member").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAtUtc)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at_utc");
            entity.Property(e => e.Delta)
                .HasPrecision(6, 2)
                .HasColumnName("delta");
            entity.Property(e => e.IsIncrease).HasColumnName("is_increase");
            entity.Property(e => e.MemberId).HasColumnName("member_id");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberScoreLogs)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("member_score_logs_member_id_fkey");
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notes_pkey");

            entity.ToTable("notes");

            entity.HasIndex(e => e.MemberId, "ix_notes_member_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasDefaultValueSql("''::text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Member).WithMany(p => p.Notes)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("fk_notes_member");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.HasIndex(e => new { e.MemberId, e.CreatedAt }, "ix_notifications_member_created").IsDescending(false, true);

            entity.HasIndex(e => new { e.MemberId, e.CreatedAt }, "ix_notifications_member_unread")
                .IsDescending(false, true)
                .HasFilter("(is_read = false)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.Text).HasColumnName("text");
        });

        modelBuilder.Entity<OrgRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("org_roles_pkey");

            entity.ToTable("org_roles");

            entity.HasIndex(e => e.Code, "ux_org_roles_code").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("organizations_pkey");

            entity.ToTable("organizations");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_resets_pkey");

            entity.ToTable("password_resets");

            entity.HasIndex(e => e.UserId, "ux_password_resets_user_active")
                .IsUnique()
                .HasFilter("(used = false)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.Used)
                .HasDefaultValue(false)
                .HasColumnName("used");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.PasswordReset)
                .HasForeignKey<PasswordReset>(d => d.UserId)
                .HasConstraintName("fk_password_resets_user");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt }, "ix_payments_org_created").IsDescending(false, true);

            entity.HasIndex(e => e.ReceiptObjectKey, "ix_payments_receipt_key");

            entity.HasIndex(e => e.Status, "ix_payments_status");

            entity.HasIndex(e => new { e.SubscriptionId, e.CreatedAt }, "ix_payments_subscription_created").IsDescending(false, true);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmountCents).HasColumnName("amount_cents");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'RUB'::text")
                .HasColumnName("currency");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.Meta)
                .HasColumnType("jsonb")
                .HasColumnName("meta");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.PaidAtUtc).HasColumnName("paid_at_utc");
            entity.Property(e => e.Provider)
                .HasDefaultValueSql("'manual'::text")
                .HasColumnName("provider");
            entity.Property(e => e.Reason)
                .HasDefaultValueSql("'new'::text")
                .HasColumnName("reason");
            entity.Property(e => e.ReceiptContentType).HasColumnName("receipt_content_type");
            entity.Property(e => e.ReceiptCreatedAt).HasColumnName("receipt_created_at");
            entity.Property(e => e.ReceiptObjectKey).HasColumnName("receipt_object_key");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'succeeded'::text")
                .HasColumnName("status");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");

            entity.HasOne(d => d.Organization).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("payments_organization_id_fkey");

            entity.HasOne(d => d.Subscription).WithMany(p => p.Payments)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("payments_subscription_id_fkey");
        });

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plans_pkey");

            entity.ToTable("plans");

            entity.HasIndex(e => e.Name, "ux_plans_name").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AllowInvites)
                .HasDefaultValue(true)
                .HasColumnName("allow_invites");
            entity.Property(e => e.AllowNewProjects)
                .HasDefaultValue(true)
                .HasColumnName("allow_new_projects");
            entity.Property(e => e.AllowNewTeams)
                .HasDefaultValue(true)
                .HasColumnName("allow_new_teams");
            entity.Property(e => e.BasePriceCents)
                .HasDefaultValue(0)
                .HasColumnName("base_price_cents");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'RUB'::text")
                .HasColumnName("currency");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MaxProjects)
                .HasDefaultValue(0)
                .HasColumnName("max_projects");
            entity.Property(e => e.MaxTeams)
                .HasDefaultValue(0)
                .HasColumnName("max_teams");
            entity.Property(e => e.MaxUsers).HasColumnName("max_users");
            entity.Property(e => e.MinProjects)
                .HasDefaultValue(0)
                .HasColumnName("min_projects");
            entity.Property(e => e.MinTeams)
                .HasDefaultValue(1)
                .HasColumnName("min_teams");
            entity.Property(e => e.MinUsers).HasColumnName("min_users");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("projects_pkey");

            entity.ToTable("projects");

            entity.HasIndex(e => e.OrganizationId, "ix_projects_org");

            entity.HasIndex(e => new { e.OrganizationId, e.Name }, "ux_projects_org_name").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CardBackgroundUrl).HasColumnName("card_background_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GitUrl).HasColumnName("git_url");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("projects_created_by_user_id_fkey");

            entity.HasOne(d => d.Organization).WithMany(p => p.Projects)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("projects_organization_id_fkey");
        });

        modelBuilder.Entity<ProjectCalendarEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_calendar_events_pkey");

            entity.ToTable("project_calendar_events");

            entity.HasIndex(e => new { e.TeamId, e.StartAt }, "ix_project_calendar_events_team_start");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByMemberId).HasColumnName("created_by_member_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.TypeId).HasColumnName("type_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByMember).WithMany(p => p.ProjectCalendarEvents)
                .HasForeignKey(d => d.CreatedByMemberId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_project_calendar_events_created_by_member");

            entity.HasOne(d => d.Task).WithMany(p => p.ProjectCalendarEvents)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_project_calendar_events_task");

            entity.HasOne(d => d.Team).WithMany(p => p.ProjectCalendarEvents)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("fk_project_calendar_events_team");
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_tasks_pkey");

            entity.ToTable("project_tasks");

            entity.HasIndex(e => e.AssignedToMemberId, "ix_project_tasks_assignee");

            entity.HasIndex(e => e.Deadline, "ix_project_tasks_deadline");

            entity.HasIndex(e => e.ProjectId, "ix_project_tasks_project");

            entity.HasIndex(e => e.TeamId, "ix_project_tasks_team");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AssignedToMemberId).HasColumnName("assigned_to_member_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByMemberId).HasColumnName("created_by_member_id");
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.IsDone)
                .HasDefaultValue(false)
                .HasColumnName("is_done");
            entity.Property(e => e.IsMissed)
                .HasDefaultValue(false)
                .HasColumnName("is_missed");
            entity.Property(e => e.Priority)
                .HasDefaultValueSql("'normal'::text")
                .HasColumnName("priority");
            entity.Property(e => e.PriorityId).HasColumnName("priority_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'todo'::text")
                .HasColumnName("status");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.AssignedToMember).WithMany(p => p.ProjectTaskAssignedToMembers)
                .HasForeignKey(d => d.AssignedToMemberId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("project_tasks_assigned_to_member_id_fkey");

            entity.HasOne(d => d.CreatedByMember).WithMany(p => p.ProjectTaskCreatedByMembers)
                .HasForeignKey(d => d.CreatedByMemberId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("project_tasks_created_by_member_id_fkey");

            entity.HasOne(d => d.PriorityNavigation).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_project_tasks_priority");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("project_tasks_project_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("project_tasks_team_id_fkey");
        });

        modelBuilder.Entity<ProjectTaskChecklistItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_task_checklist_items_pkey");

            entity.ToTable("project_task_checklist_items");

            entity.HasIndex(e => e.TaskId, "ix_task_checklist_task");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDone)
                .HasDefaultValue(false)
                .HasColumnName("is_done");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue(0)
                .HasColumnName("sort_order");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Task).WithMany(p => p.ProjectTaskChecklistItems)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("project_task_checklist_items_task_id_fkey");
        });

        modelBuilder.Entity<ProjectTeam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("project_teams_pkey");

            entity.ToTable("project_teams");

            entity.HasIndex(e => e.ProjectId, "ix_project_teams_project");

            entity.HasIndex(e => new { e.ProjectId, e.Name }, "ux_project_teams_project_name").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CardBackgroundUrl).HasColumnName("card_background_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTeams)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("project_teams_project_id_fkey");
        });

        modelBuilder.Entity<ProjectTeamMember>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.MemberId }).HasName("project_team_members_pkey");

            entity.ToTable("project_team_members");

            entity.HasIndex(e => e.MemberId, "ix_project_team_members_member");

            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.TeamRole).HasColumnName("team_role");

            entity.HasOne(d => d.Member).WithMany(p => p.ProjectTeamMembers)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("project_team_members_member_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.ProjectTeamMembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("project_team_members_team_id_fkey");
        });

        modelBuilder.Entity<PushToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("push_tokens_pkey");

            entity.ToTable("push_tokens");

            entity.HasIndex(e => e.MemberId, "ix_push_tokens_member_id");

            entity.HasIndex(e => e.Token, "ix_push_tokens_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.Platform)
                .HasMaxLength(32)
                .HasColumnName("platform");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Member).WithMany(p => p.PushTokens)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("fk_push_tokens_members");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscriptions_pkey");

            entity.ToTable("subscriptions");

            entity.HasIndex(e => new { e.OrganizationId, e.EndDate }, "ix_subscriptions_org_enddate").IsDescending(false, true);

            entity.HasIndex(e => e.OrganizationId, "ix_subscriptions_organization_id");

            entity.HasIndex(e => e.PlanId, "ix_subscriptions_plan_id");

            entity.HasIndex(e => e.StatusId, "ix_subscriptions_status");

            entity.HasIndex(e => e.StatusId, "ix_subscriptions_status_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AllowInvites)
                .HasDefaultValue(true)
                .HasColumnName("allow_invites");
            entity.Property(e => e.AllowNewProjects)
                .HasDefaultValue(true)
                .HasColumnName("allow_new_projects");
            entity.Property(e => e.AllowNewTeams)
                .HasDefaultValue(true)
                .HasColumnName("allow_new_teams");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'RUB'::text")
                .HasColumnName("currency");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.MaxMembers)
                .HasDefaultValue(0)
                .HasColumnName("max_members");
            entity.Property(e => e.MaxProjects)
                .HasDefaultValue(0)
                .HasColumnName("max_projects");
            entity.Property(e => e.MaxTeams)
                .HasDefaultValue(0)
                .HasColumnName("max_teams");
            entity.Property(e => e.Meta)
                .HasColumnType("jsonb")
                .HasColumnName("meta");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.PriceCents)
                .HasDefaultValue(0)
                .HasColumnName("price_cents");
            entity.Property(e => e.StartDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("start_date");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Organization).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("subscriptions_organization_id_fkey");

            entity.HasOne(d => d.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("subscriptions_plan_id_fkey");

            entity.HasOne(d => d.Status).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("subscriptions_status_id_fkey");
        });

        modelBuilder.Entity<SubscriptionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_items_pkey");

            entity.ToTable("subscription_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MaxQuantity)
                .HasPrecision(10, 2)
                .HasColumnName("max_quantity");
            entity.Property(e => e.MinQuantity)
                .HasPrecision(10, 2)
                .HasColumnName("min_quantity");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.PricePerUnit)
                .HasPrecision(10, 2)
                .HasColumnName("price_per_unit");
            entity.Property(e => e.Step)
                .HasPrecision(10, 2)
                .HasColumnName("step");
            entity.Property(e => e.Unit)
                .HasMaxLength(100)
                .HasColumnName("unit");
        });

        modelBuilder.Entity<SubscriptionStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("subscription_statuses_pkey");

            entity.ToTable("subscription_statuses");

            entity.HasIndex(e => e.Code, "ux_subscription_statuses_code").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<TaskDeadlineNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_deadline_notifications_pkey");

            entity.ToTable("task_deadline_notifications");

            entity.HasIndex(e => e.MemberId, "ix_task_deadline_notifications_member_id");

            entity.HasIndex(e => e.TaskId, "ix_task_deadline_notifications_task_id");

            entity.HasIndex(e => new { e.TaskId, e.MemberId, e.HoursBefore }, "ux_task_deadline_notifications_task_member_hours").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HoursBefore).HasColumnName("hours_before");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.SentAtUtc)
                .HasDefaultValueSql("now()")
                .HasColumnName("sent_at_utc");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
        });

        modelBuilder.Entity<TaskPriority>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("task_priorities_pkey");

            entity.ToTable("task_priorities");

            entity.HasIndex(e => e.Code, "task_priorities_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.DoneReward)
                .HasPrecision(6, 2)
                .HasColumnName("done_reward");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MissPenalty)
                .HasPrecision(6, 2)
                .HasColumnName("miss_penalty");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue((short)0)
                .HasColumnName("sort_order");
            entity.Property(e => e.Title).HasColumnName("title");
        });

        modelBuilder.Entity<TeamChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_chat_messages_pkey");

            entity.ToTable("team_chat_messages");

            entity.HasIndex(e => new { e.OrganizationId, e.SenderMemberId, e.Id }, "ix_team_chat_messages_org_sender_id_desc").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.OrganizationId, e.TeamId, e.Id }, "ix_team_chat_messages_org_team_id_desc").IsDescending(false, false, true);

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.PinnedAt).HasColumnName("pinned_at");
            entity.Property(e => e.PinnedByMemberId).HasColumnName("pinned_by_member_id");
            entity.Property(e => e.ReplyToMessageId).HasColumnName("reply_to_message_id");
            entity.Property(e => e.SenderMemberId).HasColumnName("sender_member_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.Text).HasColumnName("text");

            entity.HasOne(d => d.Organization).WithMany(p => p.TeamChatMessages)
                .HasForeignKey(d => d.OrganizationId)
                .HasConstraintName("team_chat_messages_organization_id_fkey");

            entity.HasOne(d => d.PinnedByMember).WithMany(p => p.TeamChatMessagePinnedByMembers)
                .HasForeignKey(d => d.PinnedByMemberId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_chat_message_pinned_by");

            entity.HasOne(d => d.ReplyToMessage).WithMany(p => p.InverseReplyToMessage)
                .HasForeignKey(d => d.ReplyToMessageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_chat_reply");

            entity.HasOne(d => d.SenderMember).WithMany(p => p.TeamChatMessageSenderMembers)
                .HasForeignKey(d => d.SenderMemberId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("team_chat_messages_sender_member_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamChatMessages)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("team_chat_messages_team_id_fkey");
        });

        modelBuilder.Entity<TeamChatMessageReaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_chat_message_reactions_pkey");

            entity.ToTable("team_chat_message_reactions");

            entity.HasIndex(e => new { e.MessageId, e.MemberId, e.Emoji }, "ix_message_reactions_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Emoji)
                .HasMaxLength(16)
                .HasColumnName("emoji");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.MessageId).HasColumnName("message_id");

            entity.HasOne(d => d.Member).WithMany(p => p.TeamChatMessageReactions)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("fk_reaction_member");

            entity.HasOne(d => d.Message).WithMany(p => p.TeamChatMessageReactions)
                .HasForeignKey(d => d.MessageId)
                .HasConstraintName("fk_reaction_message");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.CorporateEmail, "ux_users_corporate_email").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Attempt).HasColumnName("attempt");
            entity.Property(e => e.CorporateEmail)
                .HasColumnType("citext")
                .HasColumnName("corporate_email");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasDefaultValueSql("''::text")
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Patronymic).HasColumnName("patronymic");
            entity.Property(e => e.Salt).HasColumnName("salt");
            entity.Property(e => e.Surname)
                .HasDefaultValueSql("''::text")
                .HasColumnName("surname");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserLock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_locks_pkey");

            entity.ToTable("user_locks");

            entity.HasIndex(e => e.UnlockAt, "ix_user_locks_unlock_at");

            entity.HasIndex(e => e.UserId, "ix_user_locks_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.LockedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("locked_at");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.UnlockAt).HasColumnName("unlock_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserLocks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_locks_user_id_fkey");
        });

        modelBuilder.Entity<WorkSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("work_sessions_pkey");

            entity.ToTable("work_sessions");

            entity.HasIndex(e => e.OrgId, "ix_work_sessions_org_id");

            entity.HasIndex(e => new { e.OrgId, e.Status }, "ix_work_sessions_org_status");

            entity.HasIndex(e => new { e.OrgId, e.UserId }, "ix_work_sessions_org_user");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.DurationSeconds)
                .HasDefaultValue(0)
                .HasColumnName("duration_seconds");
            entity.Property(e => e.EndedAtUtc).HasColumnName("ended_at_utc");
            entity.Property(e => e.OrgId).HasColumnName("org_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Org).WithMany(p => p.WorkSessions)
                .HasForeignKey(d => d.OrgId)
                .HasConstraintName("fk_work_sessions_org");

            entity.HasOne(d => d.Project).WithMany(p => p.WorkSessions)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_work_sessions_project");

            entity.HasOne(d => d.Team).WithMany(p => p.WorkSessions)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_work_sessions_team");

            entity.HasOne(d => d.User).WithMany(p => p.WorkSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_work_sessions_user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
