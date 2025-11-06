Create a new Blazor page and component named 'WhatsAppGroups' that displays a touch-friendly list of WhatsApp group invitation links. The page should:

1. Display WhatsApp group join links in a grid or vertical layout optimized for touch devices
2. Include relevant emojis next to each WhatsApp group link (e.g. 💬, 👥)
3. Show the main WhatsApp group link prominently at the top
4. Format each link as a large, easily tappable button with:
   - Group name
   - Short description (if applicable)
   - Visual indication that it's a WhatsApp link
5. Add the page to the site's NavMenu with an appropriate icon
6. Follow Blazor component best practices and accessibility guidelines
7. Implement responsive design to work well on both mobile and desktop

Technical requirements:
- Create WhatsAppGroups.razor page in Pages folder
- Create WhatsAppGroupLink.razor component in Shared folder
- Update NavMenu.razor to include the new page
- Use Bootstrap or similar CSS framework for touch-friendly styling
- Implement proper routing configuration