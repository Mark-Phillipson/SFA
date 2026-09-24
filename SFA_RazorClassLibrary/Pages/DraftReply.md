I’m updating the draft to make it more technical and specific to the PWA architecture, including the data sources and where the app pulls its information from.

## Revised draft for the current document

Hi Jonathan,

Thanks for flagging this. I’ve now reapplied the fuller instructions to the homepage so the app purpose, installation guidance, and support information are visible again.

For clarity, the app is a Blazor-based Progressive Web App (PWA) that pulls its ride information from the San Fairy Ann Cycling Club group Google Calendar feeds. It reads the published calendar entries, filters them to the upcoming Wednesday and Saturday rides, groups them by club section, and displays the next rides on the home page in a simple, member-friendly format. The app also includes route and location information where available, and each event is associated with the relevant group so users can see where the data has come from. The club’s website provides background club information and group landing pages, while the Google calendars are the live data source for ride listings.

Technical architecture (PWA only):
- Front end: Blazor WebAssembly
- Hosting: Azure Static Web Apps
- Data sources: public Google Calendar feeds for each club group
- Local storage: browser localStorage for user-selected followed groups and preferences
- Presentation layer: homepage summary, group filtering, route links, and upcoming ride list
- User experience: PWA install flow for mobile and desktop, with a home-screen app experience

The current website URL, [Home - FairiesLittleHelper](https://www.fairieslittlehelper.online/), expires on the 15th of December. I’ll need to look into the renewal cost going forward, as it was originally set up under a one-year special offer.

For reference, the canonical link is:
polite-sand-0eb4b4703.3.azurestaticapps.net

If you’d like, I’d be very happy to make further changes once you can send me the database connection details and any relevant setup information so I can confirm exactly how it is configured and what infrastructure is involved.

Best regards,  
Mark

---

If you want, I can also tighten this into a shorter, more natural email tone or make it sound more formal for club committee correspondence.