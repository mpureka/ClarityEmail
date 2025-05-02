namespace EmailInfo
{
    class EmailParams
    {
        public required string ToName { get; set; }
        public required string ToAddress { get; set; }
        public required string Subject { get; set; }
        public required string MailBody { get; set; }
        public bool IsPlainText { get; set; } = false;
        public EmailAttachment[] Attachments { get; set; } = [];
    }
}
