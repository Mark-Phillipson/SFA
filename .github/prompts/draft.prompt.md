
# TXT (verification token at apex/root)
Resolve-DnsName -Name fairieslittlehelper.online -Type TXT
# Show text strings only
(Resolve-DnsName -Name fairieslittlehelper.online -Type TXT).Strings

# CNAME for www
Resolve-DnsName -Name www.fairieslittlehelper.online -Type CNAME

# A record for apex (if configured)
Resolve-DnsName -Name fairieslittlehelper.online -Type A

# Quick HTTP/HTTPS check (returns headers)
Invoke-WebRequest -Uri https://www.fairieslittlehelper.online -MaximumRedirection 0 -ErrorAction SilentlyContinue