**Title:** Integrate AI-powered Bot into SFA PWA (Proof of Concept)

**Description:**

We intend to add an AI-powered bot to the San Fairy Ann Cycling Club PWA.  
See the architecture and requirements in SFA_PWA_Bot_Architecture.md.

**Purpose:**
- Provide instant, accurate answers to club-related queries (membership, events, cycling routes, etc.).
- Enhance user engagement via a conversational interface.
- Serve as a proof-of-concept for AI integration in a static web app.

**Functionality:**
- Natural language understanding for user queries.
- Scripted responses using SFACC_WhatsApp_Bot_Script.txt.
- Website search and cycling route expertise (Strava/RideWithGPS).
- Quick replies for common topics.

**Architecture:**
- Frontend: New chat UI component/page in Blazor PWA.
- Backend: API endpoint (Azure App Service, free tier for PoC), bot logic, AI integration, secure API key management.
- Data sources: SFACC_WhatsApp_Bot_Script.txt, club website, Strava/RideWithGPS APIs.

**Security & Limitations:**
- API keys never exposed in client code.
- Free tier limitations: limited resources, no SLA, not for production.
- Data privacy: only public data accessed.

**Next Steps:**
1. Finalize requirements and scope.
2. Design frontend chat component.
3. Implement backend API.
4. Integrate AI and data sources.
5. Test and document.

**OpenAI Free Tier Note:**
- OpenAI offers free trial credits for new accounts, which can be used with older models (e.g., GPT-3, GPT-3.5).
- There is no ongoing free tier; after trial credits are used, all API usage is billed.
- Older models are usually cheaper than newer ones, but not free.
- See https://openai.com/pricing for details.