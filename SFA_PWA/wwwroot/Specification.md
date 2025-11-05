# San Fairy Ann Mobile Application Specification

## Overview

A mobile application for the San Fairy Ann cycling club, designed to provide easy access to club resources, ride information, and useful links.

**Architecture:**
- The initial version should be a Blazor web application with Progressive Web App (PWA) functionality, using WebAssembly only.
- The app will be hosted as a Static Web App (PWA) on Windows Azure (free hosting).
- A Blazor Hybrid App for Android and iOS may be considered in the future.
- All user settings should be saved into local storage.
- Database: We will need a JSON file to store all the links and their categories.
  
## Features

### Centralized Links
- Upcoming ride routes, published on Google Calendar (Saturday, Sunday, or Wednesday), with settings to filter by group or location.
  - https://calendar.google.com/calendar/embed?src=qtb3pcno4ugvndi8svv6s7s8s4%40group.calendar.google.com&ctz=Europe%2FLondon
  - https://calendar.google.com/calendar/embed?src=5nodk1v0lsg3onc8tkh58f4rh0%40group.calendar.google.com&ctz=Europe%2FLondon
  
  <iframe src="https://calendar.google.com/calendar/embed?src=5nodk1v0lsg3onc8tkh58f4rh0%40group.calendar.google.com&ctz=Europe%2FLondon" style="border: 0" width="800" height="600" frameborder="0" scrolling="no"></iframe>
Example Mobile View:
<iframe 
  src="https://calendar.google.com/calendar/embed?src=5nodk1v0lsg3onc8tkh58f4rh0%40group.calendar.google.com&ctz=Europe%2FLondon&mode=AGENDA" 
  style="border: 0" 
  width="400" 
  height="600" 
  frameborder="0" 
  scrolling="yes">
 moving</iframe>


- Main WhatsApp channels, including documentation on how to join.
  - https://chat.whatsapp.com/<invite_code>
  - https://chat.whatsapp.com/AbCdEfGhIjK1LmN2OpQrSt
  
- (If possible) Summaries of the latest posts from all channels. Note: WhatsApp summaries may not be feasible due to end-to-end encryption.
  
- Weather forecasts for ride days, with user-selectable services (e.g., OpenWeatherMap). Link to BBC weather app (cannot legally embed)
  
- Other resources from the San Fairy Ann website.
- Ways to purchase club kit via the website.
- Cafes and recommended stops along the routes.
- Social media channels (Facebook, Strava, Ride With GPS, Garmin, etc.).
- List of ride leaders and their emergency contact details.
- Main website links, including blog and newsletter. (https://www.sanfairyanncc.co.uk)
- Information about the Annual General Meeting.
- Recommended cycle repair shops.
- Links to your profile on Strava, Garmin Connect, etc.
- Instructions on how to renew club membership.

### Additional Features
- Use the same theme colors as the main website.
- Support desktop devices as well as mobile.
- Allow users to link to their preferred routing app (e.g., Ride with GPS, Garmin).
- Consider implementing poll features similar to WhatsApp, within the app (future enhancement).
- Encourage direct feedback via GitHub issues for the repository.
- Ability to set up reminders for upcoming rides, with summaries and weather forecasts, displayed on the dashboard only.
- Provide a filter textbox to search all available links.
- Ability to add custom links
