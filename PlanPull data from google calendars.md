## Plan: Filtered Public Calendar Events for Groups

Fetch and display only Saturday and Wednesday events from the public Google calendars listed in the JSON file, discarding past events, and filtering by user group preferences. Show ride location and route link in the existing settings page.

### Steps
1. Read group calendar URLs from SFA_PWA/wwwroot/sample-data/groups.json.
2. For each group, fetch and parse the public ICS feed, extracting only upcoming Saturday and Wednesday events.
3. For each event, extract the ride location and all route links, ensuring links are separated from the description (include multiple links if found).
4. Filter events by user group preferences, matching the existing group filter logic.
5. Display filtered events (location and route link) in the settings page (SFA_PWA/Pages), updating dynamically as group filters change.
6. Discard events as soon as their date has passed; do not persist data.

### Further Considerations
1. Confirm the ICS feed URLs for each group (may need to convert embed links to ICS links).
2. Route links must be separated from the event description; do not mix links and plain text.
3. All time zones are United Kingdom (Europe/London); no other time zone handling is required.

  the calendars and relevant information is as follows:

  [
  {
    "name": "A Group",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=4dadd39bec40fc394494897efbf0871a88bcabe5af69a714d053166cddf7e94a%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/agroup",
    "description": "Fastest group, advanced riders"
  },
  {
    "name": "Fast Inters",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=he5a6mpn3dv19ggoepejo8v8rs%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/fast-inters",
    "description": "Fast intermediate pace"
  },
  {
    "name": "Saturday 9am Inters",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=u2bg10dsho9sq4mmlv9egm2vq4%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/saturday9aminters",
    "description": "Saturday morning intermediate group"
  },
  {
    "name": "Sunday Intermediates",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=14l5adlsej6cc4adnn4kedd58o%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/sunday-intermediates",
    "description": "Sunday intermediate group"
  },
  {
    "name": "Inbetweeners",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=nu7lremlpqm1rhgufqh052qm7o%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/inbetweeners",
    "description": "Between intermediate and slower groups"
  },
  {
    "name": "B+ Group",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=2ti67dpordjct1r686ffiq64q0%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/b-group",
    "description": "Faster B group"
  },
  {
    "name": "B Group",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=qtb3pcno4ugvndi8svv6s7s8s4%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/b-group-1",
    "description": "Intermediate pace"
  },
  {
    "name": "C Group",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=lgf8bojjgr84450b26ub0m4mas%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/c-group",
    "description": "Moderate pace"
  },
  {
    "name": "Midweekers",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=k6n6eeldcafjqir3k96htmf4n8%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/midweekers",
    "description": "Midweek rides Wednesdays various meeting places"
  },
  {
    "name": "D+ Group",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=5nodk1v0lsg3onc8tkh58f4rh0%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/d-group",
    "description": "30-40 miles, 12-14 mph"
  },
  {
    "name": "D Group",
  "calendarUrl": "https://calendar.google.com/calendar/embed?src=5nodk1v0lsg3onc8tkh58f4rh0%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/d-group-1",
    "description": "Social, steady pace"
  },
  {
    "name": "EasyRiders",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=u4rq2o39e5v84ee9ngrdspio40%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/easyriders",
    "description": "Gentle rides"
  },
  {
    "name": "EasyRiders+",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=m0qddgejip3mibdt2uvoadffcg%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/easyriders-1",
    "description": "Gentle rides plus"
  },
  {
    "name": "Mountain Bike Group",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=clod8b8sh1rs96dtg6fpsrfjek%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/mountain-bike-group",
    "description": "Off-road rides"
  },
  {
    "name": "Virtual Riding",
    "calendarUrl": "https://calendar.google.com/calendar/embed?src=9ujq6dpd3pcvhpifpp8end53ek%40group.calendar.google.com&ctz=Europe%2FLondon",
    "infoUrl": "https://www.sanfairyanncc.co.uk/virtual-riding",
    "description": "Online rides using Zwift/Discord"
  }
]
