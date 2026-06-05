using steptreck.API.Models;
using steptreck.Domain.DTOs.InviteDTO;
using steptreck.Domain.DTOs.TeamDTOs;
using System.Net;
using System.Net.Mail;

namespace steptreck.API.Infrastructure.Email
{
    public class EmailHelper
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly AppDbContext _context;
        private readonly IConfiguration _conf;

        public EmailHelper(IConfiguration configuration, AppDbContext context, IConfiguration conf)
        {
            _context = context;

            var host = configuration["EmailSettings:SmtpServer"];
            var portStr = configuration["EmailSettings:SmtpPort"];
            var user = configuration["EmailSettings:SmtpUser"];
            var pass = configuration["EmailSettings:SmtpPass"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass))
            {
                throw new InvalidOperationException("EmailSettings не настроены. Проверь appsettings.json");
            }

            if (!int.TryParse(portStr, out var port))
                throw new InvalidOperationException("EmailSettings:SmtpPort должен быть числом");

            _smtpServer = host;
            _smtpPort = port;
            _smtpUser = user;
            _smtpPass = pass;
            _conf = conf;
        }



        public async Task SendEmailAsync(string to, string subject, string body)
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.EnableSsl = true;

                var message = new MailMessage(_smtpUser, to, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(message);
            }
        }
        public async Task SendReceiptEmailAsync(
            string to,
            long paymentId,
            string receiptDownloadUrl,
            string? orgName = null)
                {
                    var subject = $"StepTreck — чек оплаты #{paymentId}";

                    var safeOrg = WebUtility.HtmlEncode(orgName ?? "вашей организации");
                    var safeUrl = WebUtility.HtmlEncode(receiptDownloadUrl);

                    var body = $@"
        <div style='font-family:Arial,sans-serif;line-height:1.5;color:#111'>
          <h2 style='margin:0 0 8px'>Чек оплаты сформирован</h2>
          <div style='color:#666;margin-bottom:12px'>Организация: <b>{safeOrg}</b></div>
          <div style='margin-bottom:16px'>Номер операции: <b>#{paymentId}</b></div>

          <a href='{safeUrl}' style='display:inline-block;padding:10px 14px;border-radius:10px;
             background:#6d28d9;color:#fff;text-decoration:none'>
             Скачать чек
          </a>

          <div style='margin-top:18px;color:#666;font-size:12px'>
            Если кнопка не работает, скопируйте ссылку: <br/>
            <span>{safeUrl}</span>
          </div>
        </div>";

            await SendEmailAsync(to, subject, body);
        }


        public async Task SendConfirmCode(string email, string code)
        {
            var subject = "Код подтверждения входа";
            var body = $@"
<!doctype html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>Код подтверждения</title>

  <style>
    /* Часть клиентов игнорит <style>, но те, кто поддерживают, сделают ещё красивее */
    @media (max-width: 520px) {{
      .container {{ width: 100% !important; }}
      .pad {{ padding: 18px !important; }}
      .h1 {{ font-size: 22px !important; line-height: 28px !important; }}
      .code {{ font-size: 34px !important; letter-spacing: 6px !important; }}
    }}
  </style>
</head>

<body style=""margin:0;padding:0;background:#070A17;"">
  <!-- Preheader (текст в превью) -->
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">
    Ваш код подтверждения: {code}. Действует 10 минут.
  </div>

  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background:#070A17;"">
    <tr>
      <td align=""center"" style=""padding:26px 12px;"">

        <!-- Background “aurora” -->
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" class=""container"" width=""640"" style=""width:640px;max-width:640px;"">
          <tr>
            <td style=""padding:0 0 14px 0;"">
              <div style=""font-family:Segoe UI,Arial,sans-serif;color:#9CA3AF;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;text-align:center;"">
                STEPTRECK • SECURITY
              </div>
            </td>
          </tr>

          <tr>
            <td style=""padding:0;"">
              <!-- Gradient border wrapper -->
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:26px;background:linear-gradient(135deg,#8B5CF6 0%,#22D3EE 45%,#FB7185 100%);padding:2px;"">
                <tr>
                  <td style=""border-radius:24px;background:#0B1023;"">

                    <!-- Header -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding:26px 26px 10px 26px;"" class=""pad"">

                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                            <tr>
                              <td style=""vertical-align:middle;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#E5E7EB;font-size:12px;letter-spacing:0.14em;text-transform:uppercase;"">
                                  Подтверждение входа
                                </div>
                                <div class=""h1"" style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#FFFFFF;font-size:28px;line-height:34px;font-weight:800;"">
                                  Ваш одноразовый код
                                </div>
                                <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#A5B4FC;font-size:14px;line-height:20px;"">
                                  Введите код ниже, чтобы завершить вход. Он действует <b style=""color:#E5E7EB;"">10 минут</b>.
                                </div>
                              </td>
                              <td align=""right"" style=""vertical-align:middle;width:92px;"">
                                <div style=""width:64px;height:64px;border-radius:18px;background:linear-gradient(180deg,rgba(139,92,246,.22),rgba(34,211,238,.14));border:1px solid rgba(255,255,255,.10);text-align:center;line-height:64px;font-size:30px;"">
                                  🔒
                                </div>
                              </td>
                            </tr>
                          </table>

                        </td>
                      </tr>
                    </table>

                    <!-- Divider -->
                    <div style=""height:1px;background:linear-gradient(90deg,rgba(255,255,255,0),rgba(255,255,255,.14),rgba(255,255,255,0));""></div>

                    <!-- Code block -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding:22px 26px 6px 26px;"" class=""pad"">
                          <div style=""font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;"">
                            Код подтверждения
                          </div>
                        </td>
                      </tr>

                      <tr>
                        <td align=""center"" style=""padding:10px 26px 18px 26px;"" class=""pad"">

                          <!-- “Neon” code card -->
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""border-radius:20px;background:linear-gradient(180deg,rgba(255,255,255,.06),rgba(255,255,255,.02));border:1px solid rgba(255,255,255,.10);box-shadow:0 18px 50px rgba(0,0,0,.45);width:100%;max-width:520px;"">
                            <tr>
                              <td align=""center"" style=""padding:18px 14px;"">
                                <div class=""code"" style=""font-family:'Courier New',Consolas,monospace;font-size:44px;letter-spacing:10px;font-weight:800;color:#FFFFFF;text-shadow:0 0 22px rgba(34,211,238,.42),0 0 26px rgba(139,92,246,.35);"">
                                  {code}
                                </div>

                                <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#CBD5E1;font-size:13px;line-height:18px;"">
                                  Совет: дважды кликните по коду, чтобы быстро выделить.
                                </div>
                              </td>
                            </tr>
                          </table>

                        </td>
                      </tr>

                      <!-- Safety note -->
                      <tr>
                        <td style=""padding:0 26px 22px 26px;"" class=""pad"">
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:16px;background:rgba(251,191,36,.10);border:1px solid rgba(251,191,36,.22);"">
                            <tr>
                              <td style=""padding:14px 14px;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#FDE68A;font-size:13px;line-height:18px;"">
                                  ⚠️ <b style=""color:#FFF7ED;"">Никому не сообщайте этот код</b>.
                                  Служба поддержки никогда не попросит его.
                                </div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style=""padding:0 26px 26px 26px;"" class=""pad"">
                          <div style=""font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:12px;line-height:18px;"">
                            Если вы не запрашивали вход — просто проигнорируйте письмо. Для безопасности можно сменить пароль.
                          </div>
                          <div style=""margin-top:12px;font-family:Segoe UI,Arial,sans-serif;color:#64748B;font-size:11px;line-height:16px;"">
                            Это автоматическое сообщение. Пожалуйста, не отвечайте на него.
                          </div>
                        </td>
                      </tr>
                    </table>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Tiny footer -->
          <tr>
            <td style=""padding:14px 0 0 0;text-align:center;font-family:Segoe UI,Arial,sans-serif;color:#475569;font-size:11px;line-height:16px;"">
              © {DateTime.UtcNow:yyyy} StepTreck. Все права защищены.
            </td>
          </tr>
        </table>

      </td>
    </tr>
  </table>
</body>
</html>";


            await SendEmailAsync(email, subject, body);
        }

        public async Task SendInvite(string email, string link, SendInviteDto info)
        {
            var subject = $"Приглашение в {info.OrgName} • StepTreck";

            string H(string? s) => WebUtility.HtmlEncode(s ?? "");

            var orgName = H(info.OrgName);
            var sender = H(info.FullNameSender);
            var safeLink = H(link);

            var body = $@"
<!doctype html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>Приглашение в организацию</title>

  <style>
    @media (max-width: 560px) {{
      .container {{ width: 100% !important; }}
      .pad {{ padding: 18px !important; }}
      .h1 {{ font-size: 22px !important; line-height: 28px !important; }}
      .sub {{ font-size: 14px !important; line-height: 20px !important; }}
      .btn {{ display:block !important; width:100% !important; }}
      .chip {{ display:block !important; width:100% !important; }}
    }}
  </style>
</head>

<body style=""margin:0;padding:0;background:#070A17;"">
  <!-- Preheader -->
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">
    Вас пригласили в {orgName}. Нажмите «Принять приглашение», чтобы присоединиться.
  </div>

  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background:#070A17;"">
    <tr>
      <td align=""center"" style=""padding:26px 12px;"">

        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" class=""container"" width=""680"" style=""width:680px;max-width:680px;"">
          <tr>
            <td style=""padding:0 0 14px 0;"">
              <div style=""font-family:Segoe UI,Arial,sans-serif;color:#9CA3AF;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;text-align:center;"">
                STEPTRECK • INVITATION
              </div>
            </td>
          </tr>

          <!-- Gradient frame -->
          <tr>
            <td style=""padding:0;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:28px;background:linear-gradient(135deg,#22D3EE 0%,#8B5CF6 40%,#FB7185 100%);padding:2px;"">
                <tr>
                  <td style=""border-radius:26px;background:#0B1023;"">

                    <!-- Header -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:28px 28px 16px 28px;"">

                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                            <tr>
                              <td style=""vertical-align:middle;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#A5B4FC;font-size:12px;letter-spacing:0.14em;text-transform:uppercase;"">
                                  Приглашение в организацию
                                </div>

                                <div class=""h1"" style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#FFFFFF;font-size:30px;line-height:36px;font-weight:900;"">
                                  Добро пожаловать в <span style=""color:#E9D5FF;"">{orgName}</span>
                                </div>

                                <div class=""sub"" style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#CBD5E1;font-size:15px;line-height:22px;"">
                                  {sender} приглашает вас присоединиться к команде {orgName}.
                                  Нажмите кнопку ниже — и вы внутри.
                                </div>
                              </td>

                              <td align=""right"" style=""vertical-align:middle;width:110px;"">
                                <div style=""width:74px;height:74px;border-radius:22px;background:radial-gradient(circle at 30% 25%, rgba(34,211,238,.35), rgba(139,92,246,.20) 45%, rgba(251,113,133,.14));border:1px solid rgba(255,255,255,.10);text-align:center;line-height:74px;font-size:34px;"">
                                  🚀
                                </div>
                              </td>
                            </tr>
                          </table>

                        </td>
                      </tr>
                    </table>

                    <!-- Divider -->
                    <div style=""height:1px;background:linear-gradient(90deg,rgba(255,255,255,0),rgba(255,255,255,.14),rgba(255,255,255,0));""></div>

                    <!-- Chips / Info -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:18px 28px 6px 28px;"">
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                            <tr>
                              <td style=""padding:6px 6px 6px 0;"" class=""chip"">
                                <div style=""border-radius:16px;background:linear-gradient(180deg,rgba(255,255,255,.06),rgba(255,255,255,.02));border:1px solid rgba(255,255,255,.10);padding:12px 12px;font-family:Segoe UI,Arial,sans-serif;color:#E5E7EB;font-size:13px;line-height:18px;"">
                                  <div style=""color:#94A3B8;font-size:11px;letter-spacing:.12em;text-transform:uppercase;margin-bottom:6px;"">Организация</div>
                                  <b style=""font-size:14px;"">{orgName}</b>
                                </div>
                              </td>
                            </tr>
                          </table>

                          <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:12px;line-height:18px;"">
                            Если вы ещё не зарегистрированы, сначала создайте аккаунт, затем вернитесь по этой ссылке — приглашение будет активным.
                          </div>
                        </td>
                      </tr>
                    </table>

                    <!-- CTA Button -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td align=""center"" class=""pad"" style=""padding:18px 28px 10px 28px;"">
                          <!-- button wrapper -->
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" class=""btn"" style=""border-radius:18px;background:linear-gradient(135deg,#22D3EE 0%,#8B5CF6 45%,#FB7185 100%);padding:2px;display:inline-block;"">
                            <tr>
                              <td align=""center"" style=""border-radius:16px;background:#0B1023;"">
                                <a href=""{safeLink}""
                                   style=""display:inline-block;padding:14px 22px;font-family:Segoe UI,Arial,sans-serif;font-size:14px;font-weight:800;letter-spacing:.06em;text-transform:uppercase;color:#FFFFFF;text-decoration:none;border-radius:16px;"">
                                  Принять приглашение
                                </a>
                              </td>
                            </tr>
                          </table>

                          <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:12px;line-height:18px;"">
                            Ссылка одноразовая. Если кнопка не работает — используйте адрес ниже.
                          </div>
                        </td>
                      </tr>
                    </table>

                    <!-- Fallback link -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:0 28px 16px 28px;"">
                          <div style=""border-radius:16px;background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.10);padding:12px 12px;font-family:Consolas,'Courier New',monospace;color:#E5E7EB;font-size:12px;line-height:18px;word-break:break-all;"">
                            {safeLink}
                          </div>
                        </td>
                      </tr>
                    </table>

                    <!-- Safety note -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:0 28px 24px 28px;"">
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:16px;background:rgba(251,191,36,.10);border:1px solid rgba(251,191,36,.22);"">
                            <tr>
                              <td style=""padding:14px 14px;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#FDE68A;font-size:13px;line-height:18px;"">
                                  ⚠️ <b style=""color:#FFF7ED;"">Безопасность:</b> не пересылайте эту ссылку третьим лицам.
                                  Если вы не ожидаете приглашение — просто проигнорируйте письмо.
                                </div>
                              </td>
                            </tr>
                          </table>

                          <div style=""margin-top:12px;font-family:Segoe UI,Arial,sans-serif;color:#64748B;font-size:11px;line-height:16px;"">
                            Это автоматическое сообщение StepTreck. Пожалуйста, не отвечайте на него.
                          </div>
                        </td>
                      </tr>
                    </table>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- tiny footer -->
          <tr>
            <td style=""padding:14px 0 0 0;text-align:center;font-family:Segoe UI,Arial,sans-serif;color:#475569;font-size:11px;line-height:16px;"">
              © {DateTime.UtcNow:yyyy} StepTreck • Сделано с любовью к трекингу и порядку ✨
            </td>
          </tr>

        </table>

      </td>
    </tr>
  </table>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }
        public async Task SendTeamAssignmentAsync(string email, TeamAssignmentEmailDto info)
        {
            var subject = $"Вы добавлены в команду {info.TeamName} • StepTreck";

            string H(string? s) => WebUtility.HtmlEncode(s ?? "");

            var recipient = H(info.RecipientFullName);
            var team = H(info.TeamName);
            var role = H(info.RoleTitle);
            var project = H(info.ProjectName);

            var projectLine = string.IsNullOrWhiteSpace(info.ProjectName)
                ? ""
                : $@"<div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#CBD5E1;font-size:14px;line-height:20px;"">
              Проект: <b style=""color:#E9D5FF;"">{project}</b>
            </div>";

            var body = $@"
<!doctype html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>Назначение в команду</title>
</head>

<body style=""margin:0;padding:0;background:#070A17;"">
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">
    Вы добавлены в команду {team}. Роль: {role}.
  </div>

  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background:#070A17;"">
    <tr>
      <td align=""center"" style=""padding:26px 12px;"">

        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""680"" style=""width:680px;max-width:680px;"">
          <tr>
            <td style=""padding:0 0 14px 0;"">
              <div style=""font-family:Segoe UI,Arial,sans-serif;color:#9CA3AF;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;text-align:center;"">
                STEPTRECK • TEAM
              </div>
            </td>
          </tr>

          <tr>
            <td style=""padding:0;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:28px;background:linear-gradient(135deg,#22D3EE 0%,#8B5CF6 45%,#FB7185 100%);padding:2px;"">
                <tr>
                  <td style=""border-radius:26px;background:#0B1023;"">

                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding:28px 28px 16px 28px;"">

                          <div style=""font-family:Segoe UI,Arial,sans-serif;color:#A5B4FC;font-size:12px;letter-spacing:0.14em;text-transform:uppercase;"">
                            Добро пожаловать
                          </div>

                          <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#FFFFFF;font-size:30px;line-height:36px;font-weight:900;"">
                            {recipient}, вы в команде ✅
                          </div>

                          <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#CBD5E1;font-size:15px;line-height:22px;"">
                            Вас добавили в команду <b style=""color:#E9D5FF;"">{team}</b>.
                            Желаем продуктивной работы и отличных результатов ✨
                          </div>
                          {projectLine}

                        </td>
                      </tr>
                    </table>

                    <div style=""height:1px;background:linear-gradient(90deg,rgba(255,255,255,0),rgba(255,255,255,.14),rgba(255,255,255,0));""></div>

                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td style=""padding:18px 28px 24px 28px;"">
                          <div style=""border-radius:16px;background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.10);padding:14px 14px;"">
                            <div style=""font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:11px;letter-spacing:.12em;text-transform:uppercase;margin-bottom:6px;"">
                              Ваша роль
                            </div>
                            <div style=""font-family:Segoe UI,Arial,sans-serif;color:#E5E7EB;font-size:16px;font-weight:800;"">
                              {role}
                            </div>
                          </div>

                          <div style=""margin-top:12px;font-family:Segoe UI,Arial,sans-serif;color:#64748B;font-size:11px;line-height:16px;"">
                            Это автоматическое сообщение StepTreck. Пожалуйста, не отвечайте на него.
                          </div>
                        </td>
                      </tr>
                    </table>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <tr>
            <td style=""padding:14px 0 0 0;text-align:center;font-family:Segoe UI,Arial,sans-serif;color:#475569;font-size:11px;line-height:16px;"">
              © {DateTime.UtcNow:yyyy} StepTreck
            </td>
          </tr>
        </table>

      </td>
    </tr>
  </table>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }
        public async Task SendPasswordReset(string email, string token)
        {
            var link = $"{_conf["App:FrontendUrl"]}/reset-password?token={Uri.EscapeDataString(token)}";

            var subject = "Сброс пароля • StepTreck";

            string H(string? s) => WebUtility.HtmlEncode(s ?? "");

            var safeLink = H(link);

            var body = $@"
<!doctype html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <meta name=""x-apple-disable-message-reformatting"">
  <title>Сброс пароля</title>

  <style>
    @media (max-width: 560px) {{
      .container {{ width: 100% !important; }}
      .pad {{ padding: 18px !important; }}
      .h1 {{ font-size: 22px !important; line-height: 28px !important; }}
      .sub {{ font-size: 14px !important; line-height: 20px !important; }}
      .btn {{ display:block !important; width:100% !important; }}
    }}
  </style>
</head>

<body style=""margin:0;padding:0;background:#070A17;"">
  <!-- Preheader -->
  <div style=""display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;"">
    Ссылка для сброса пароля. Действует 1 час.
  </div>

  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background:#070A17;"">
    <tr>
      <td align=""center"" style=""padding:26px 12px;"">

        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" class=""container"" width=""680"" style=""width:680px;max-width:680px;"">
          <tr>
            <td style=""padding:0 0 14px 0;"">
              <div style=""font-family:Segoe UI,Arial,sans-serif;color:#9CA3AF;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;text-align:center;"">
                STEPTRECK • SECURITY
              </div>
            </td>
          </tr>

          <!-- Gradient frame -->
          <tr>
            <td style=""padding:0;"">
              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:28px;background:linear-gradient(135deg,#FB7185 0%,#8B5CF6 45%,#22D3EE 100%);padding:2px;"">
                <tr>
                  <td style=""border-radius:26px;background:#0B1023;"">

                    <!-- Header -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:28px 28px 16px 28px;"">

                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                            <tr>
                              <td style=""vertical-align:middle;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#A5B4FC;font-size:12px;letter-spacing:0.14em;text-transform:uppercase;"">
                                  Сброс пароля
                                </div>

                                <div class=""h1"" style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#FFFFFF;font-size:30px;line-height:36px;font-weight:900;"">
                                  Создайте новый пароль
                                </div>

                                <div class=""sub"" style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#CBD5E1;font-size:15px;line-height:22px;"">
                                  Мы получили запрос на сброс пароля для вашего аккаунта.
                                  Нажмите кнопку ниже, чтобы перейти к созданию нового пароля.
                                </div>
                              </td>

                              <td align=""right"" style=""vertical-align:middle;width:110px;"">
                                <div style=""width:74px;height:74px;border-radius:22px;background:radial-gradient(circle at 30% 25%, rgba(251,113,133,.32), rgba(139,92,246,.20) 45%, rgba(34,211,238,.12));border:1px solid rgba(255,255,255,.10);text-align:center;line-height:74px;font-size:34px;"">
                                  🔄
                                </div>
                              </td>
                            </tr>
                          </table>

                        </td>
                      </tr>
                    </table>

                    <!-- Divider -->
                    <div style=""height:1px;background:linear-gradient(90deg,rgba(255,255,255,0),rgba(255,255,255,.14),rgba(255,255,255,0));""></div>

                    <!-- CTA -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td align=""center"" class=""pad"" style=""padding:18px 28px 10px 28px;"">

                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" class=""btn"" style=""border-radius:18px;background:linear-gradient(135deg,#FB7185 0%,#8B5CF6 45%,#22D3EE 100%);padding:2px;display:inline-block;"">
                            <tr>
                              <td align=""center"" style=""border-radius:16px;background:#0B1023;"">
                                <a href=""{safeLink}""
                                   style=""display:inline-block;padding:14px 22px;font-family:Segoe UI,Arial,sans-serif;font-size:14px;font-weight:900;letter-spacing:.06em;text-transform:uppercase;color:#FFFFFF;text-decoration:none;border-radius:16px;"">
                                  Сбросить пароль
                                </a>
                              </td>
                            </tr>
                          </table>

                          <div style=""margin-top:10px;font-family:Segoe UI,Arial,sans-serif;color:#94A3B8;font-size:12px;line-height:18px;"">
                            Ссылка действует <b style=""color:#E5E7EB;"">1 час</b>. Если кнопка не работает — используйте адрес ниже.
                          </div>

                        </td>
                      </tr>
                    </table>

                    <!-- Fallback link -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:0 28px 16px 28px;"">
                          <div style=""border-radius:16px;background:rgba(255,255,255,.04);border:1px solid rgba(255,255,255,.10);padding:12px 12px;font-family:Consolas,'Courier New',monospace;color:#E5E7EB;font-size:12px;line-height:18px;word-break:break-all;"">
                            {safeLink}
                          </div>
                        </td>
                      </tr>
                    </table>

                    <!-- Safety note -->
                    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
                      <tr>
                        <td class=""pad"" style=""padding:0 28px 24px 28px;"">
                          <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""border-radius:16px;background:rgba(251,191,36,.10);border:1px solid rgba(251,191,36,.22);"">
                            <tr>
                              <td style=""padding:14px 14px;"">
                                <div style=""font-family:Segoe UI,Arial,sans-serif;color:#FDE68A;font-size:13px;line-height:18px;"">
                                  ⚠️ <b style=""color:#FFF7ED;"">Важно:</b> если вы не запрашивали сброс пароля — просто проигнорируйте это письмо.
                                  Для безопасности можно сменить пароль в настройках аккаунта.
                                </div>
                              </td>
                            </tr>
                          </table>

                          <div style=""margin-top:12px;font-family:Segoe UI,Arial,sans-serif;color:#64748B;font-size:11px;line-height:16px;"">
                            Это автоматическое сообщение StepTreck. Пожалуйста, не отвечайте на него.
                          </div>
                        </td>
                      </tr>
                    </table>

                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- tiny footer -->
          <tr>
            <td style=""padding:14px 0 0 0;text-align:center;font-family:Segoe UI,Arial,sans-serif;color:#475569;font-size:11px;line-height:16px;"">
              © {DateTime.UtcNow:yyyy} StepTreck • Сделано с любовью к трекингу и порядку ✨
            </td>
          </tr>

        </table>

      </td>
    </tr>
  </table>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }





    }
}
