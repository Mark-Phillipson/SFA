
Can you pull out the data from this pdf and create a JSON so we can use it in a Blazor component?


 if so can we use the following:

 

 Which is a pdf document as the source for a new Blazor component called D Group Start Points.

  please don't build the component at this stage just the JSON file

  # create target folders if needed
New-Item -ItemType Directory -Path .\SFA_PWA\wwwroot\assets -Force
New-Item -ItemType Directory -Path .\SFA_PWA\wwwroot\data -Force

# copy local PDF into repo assets
Copy-Item -Path 'C:\Users\MPhil\source\repos\SFA\SFA_PWA\D-MW Group Start points v 29Aug2023.pdf' -Destination .\SFA_PWA\wwwroot\assets\d-group.pdf