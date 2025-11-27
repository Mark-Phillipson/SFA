# Running Both Projects in Visual Studio Code

To launch both the Blazor PWA (SFA_PWA) and the WebAPI backend (SFA_WebAPI) in development:

1. Open two terminals in VS Code.
2. In the first terminal, run:
	```pwsh
	dotnet run --project SFA_PWA/SFA_PWA.csproj
	```
3. In the second terminal, run:
	```pwsh
	dotnet run --project SFA_WebAPI/SFA_WebAPI.csproj
	```
4. Make sure your Blazor app’s API calls use the correct backend URL (http://localhost:5216/api/bot/chat).

You can copy and paste these commands directly into your VS Code terminals.
# 🚴‍♂️ San Fairy Ann Cycling Club App

Welcome to the proposed mobile app for the San Fairy Ann Cycling Club! 🎉

## 🌟 Overview
A modern Blazor Progressive Web App (PWA) designed to help club members and guests easily access ride info, club resources, and useful cycling links. Works on desktop and mobile, and saves your settings locally for convenience.

## 🏆 Features
- 📅 **Ride Info:** Upcoming routes and schedules from Google Calendar
- 🔗 **Links:** Quick access to WhatsApp channels, social media, club kit shop, recommended cafes, repair shops, and more
- 🌦️ **Weather:** Forecasts for ride days, with your favorite weather service
- ⚙️ **Settings:** Save your preferences and filter rides by group or location
- 🔍 **Search:** Instantly find any link or resource
- ➕ **Custom Links:** Add your own favorite cycling resources
- 🆘 **Help & Feedback:** Direct link to GitHub for reporting issues or suggestions

## 🚀 How to Use
1. **Browse the navigation menu** to explore all features
2. **Check the Calendars page** for upcoming rides
3. **Visit the Links page** for club resources, WhatsApp, Strava, Garmin, and more
4. **Check the Weather page** for ride forecasts
5. **Customize your experience** in Settings
6. **Search or add links** to make the app your own

## 💡 Tips
- The app works offline and saves your settings in your browser
- You can install it on your phone or desktop for quick access
- For feedback or help, use the GitHub link in the app menu

## 🤝 Contributing
We welcome feedback and contributions! Please open an issue or pull request on GitHub.

## 📝 License
This project is open source and free for all club members and cycling enthusiasts.

---

Made with ❤️ for the San Fairy Ann Cycling Club 🚴‍♀️🚴‍♂️🚴
