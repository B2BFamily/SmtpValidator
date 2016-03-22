# SmtpValidator
Validate smtp credentials before sending email on c#

For validation email and password invoke:
   
    var login = "testtesttest@mail.ru";
    var password = "testtesttest";
    var smtpServer = "smtp.mail.ru";
    var smtpPort = 465;
    var enableSsl = true;
    var isValid = SmtpHelper.ValidateCredentials(login, password, smtpServer, smtpPort, enableSsl);
    
    
This lib used in https://b2bfamily.com for validation user credentials.
