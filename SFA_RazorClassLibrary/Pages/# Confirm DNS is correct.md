# Confirm DNS is correct
Resolve-DnsName -Name www.fairieslittlehelper.online -Type CNAME
Resolve-DnsName -Name fairieslittlehelper.online -Type TXT

# See HTTP/HTTPS response and any redirect
Invoke-WebRequest -Uri http://fairieslittlehelper.online -MaximumRedirection 0 -ErrorAction SilentlyContinue
Invoke-WebRequest -Uri https://www.fairieslittlehelper.online -MaximumRedirection 0 -ErrorAction SilentlyContinue