namespace KT_Learn.Models.Enum
{
    // PostgreSQL enum draft_review_status. Npgsql переводит имена в snake_case:
    // Pending -> 'pending', Approved -> 'approved', Rejected -> 'rejected'.
    public enum DraftReviewStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
