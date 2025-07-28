# Contoso Hotels AI Concierge Implementation Plan

## Phase 1: Foundation Setup

### 1.1 Project Dependencies and Configuration
- **Upgrade NuGet packages** to add Semantic Kernel and AI services
- **Add Microsoft.SemanticKernel** package
- **Add Microsoft.SemanticKernel.Connectors.OpenAI** package
- **Add SignalR** for real-time chat functionality
- **Configure AI services** in `appsettings.json`

### 1.2 Database Extensions
- **Create conversation history model** to store chat sessions
- **Add agent interaction logging** for analytics
- **Create EF migration** for new tables

## Phase 2: UI Chat Interface

### 2.1 Chat Widget Component
- **Create floating chat button** in bottom-right corner of all pages
- **Design chat interface** matching Contoso Hotels branding (#172232, #d3b168)
- **Implement responsive chat window** with:
  - Message history display
  - Typing indicators
  - Agent status indicators
  - Collapsable for users not wanting to use, state should be saved for future sessions
- **Add to `_Layout.cshtml`** for site-wide availability

### 2.2 Frontend JavaScript
- **SignalR client integration** for real-time messaging
- **Chat UI JavaScript** for message handling and display
- **Conversation state management** on client-side
- **Auto-scroll and notification** features

## Phase 3: Semantic Kernel Agent Orchestrator

### 3.1 Core Orchestrator Setup
- **Create `Services/AIConciergeService.cs`** as main orchestrator
- **Implement agent routing logic** based on user intent
- **Configure Semantic Kernel** with plugins for each agent
- **Add conversation context management**

### 3.2 Intent Recognition
- **Create intent classification** system
- **Define conversation patterns** for:
  - Room booking requests
  - Room service orders  
  - Housekeeping requests
  - General inquiries
- **Implement fallback mechanisms** for unclear intents

### 3.3 Context Management
- **Session state handling** across agent handoffs
- **Conversation history integration**
- **User preference tracking**

## Phase 4: Room Booking Agent

### 4.1 Booking Agent Implementation
- **Create `Services/Agents/RoomBookingAgent.cs`**
- **Integrate with existing room search logic** from `RoomsController`
- **Implement conversation flow** for:
  - Destination inquiry
  - Date selection and validation
  - Guest count and preferences
  - Room type recommendations
  - Price filtering
  - Booking confirmation

### 4.2 Natural Language Processing
- **Date parsing and validation** (flexible date formats)
- **Location matching** against available cities
- **Budget interpretation** and price range mapping
- **Room preference extraction** (amenities, view type, etc.)

### 4.3 Integration Points
- **Leverage existing `Room` model** and database queries
- **Use `Booking` model** for reservation creation
- **Connect to `Customer` model** for guest information

## Phase 5: Room Service Agent

### 5.1 Room Service Agent Implementation  
- **Create `Services/Agents/RoomServiceAgent.cs`**
- **Build menu system** with categories and items
- **Implement ordering conversation flow**:
  - Menu browsing and recommendations
  - Item selection and customization
  - Quantity and timing preferences
  - Order confirmation and tracking

### 5.2 Menu Integration
- **Create menu data structure** (could be seeded data or separate table)
- **Implement search and filtering** by dietary restrictions, price, etc.
- **Add recommendation engine** based on popular items

### 5.3 Order Management
- **Integrate with existing `RoomService` model**
- **Handle order modifications** and cancellations
- **Provide order status updates**

## Phase 6: Housekeeping Agent

### 6.1 Housekeeping Agent Implementation
- **Create `Services/Agents/HousekeepingAgent.cs`**
- **Build service request system** for common housekeeping needs
- **Implement request conversation flow**:
  - Service type identification
  - Scheduling preferences
  - Special instructions
  - Request confirmation

### 6.2 Service Categories
- **Leverage existing `HousekeepingRequestType` enum**
- **Add intelligent service recommendations**
- **Handle time-sensitive requests** (towels, toiletries)

### 6.3 Scheduling Integration
- **Room availability checking**
- **Staff scheduling coordination**
- **Priority handling** for urgent requests

## Phase 7: Backend API and Controllers

### 7.1 Chat API Controller
- **Create `Controllers/ChatController.cs`**
- **Implement WebSocket/SignalR endpoints**
- **Add message validation and sanitization**
- **Handle file uploads** for images/documents

### 7.2 Agent Management APIs
- **Agent status endpoints**
- **Conversation history APIs**
- **Analytics and reporting endpoints**

## Phase 8: Security and Data Protection

### 8.1 Security Implementation
- **Input validation and sanitization**
- **Rate limiting** for chat interactions
- **Personal data protection** compliance
- **Secure session management**

### 8.2 Authentication Integration
- **Guest identification** for existing bookings
- **Anonymous chat support** for new customers
- **Staff authentication** for admin features

## Phase 9: Admin Dashboard

### 9.1 Monitoring Interface
- **Extend `ManagerController`** with AI concierge analytics
- **Real-time chat monitoring** for staff intervention
- **Conversation history search** and review
- **Agent performance metrics**

### 9.2 Configuration Management
- **Agent settings configuration**
- **Menu and service updates**
- **AI model parameter tuning**

## Phase 10: Testing and Optimization

### 10.1 Testing Strategy
- **Unit tests** for each agent
- **Integration tests** for conversation flows
- **Performance testing** for concurrent users
- **User acceptance testing** with sample conversations

### 10.2 Performance Optimization
- **Response time optimization**
- **Caching strategy** for common queries
- **Database query optimization**
- **AI model response tuning**

## Implementation Timeline

- **Phase 1-2**: Foundation and UI (Week 1)
- **Phase 3**: Orchestrator setup (Week 1-2)
- **Phase 4**: Room Booking Agent (Week 2-3)
- **Phase 5**: Room Service Agent (Week 3)
- **Phase 6**: Housekeeping Agent (Week 3-4)
- **Phase 7-8**: APIs and Security (Week 4)
- **Phase 9**: Admin Dashboard (Week 4-5)
- **Phase 10**: Testing and Optimization (Week 5-6)

## Key Technical Decisions

1. **Semantic Kernel** as the AI orchestration framework
2. **SignalR** for real-time chat functionality
3. **Intent-based routing** between specialized agents
4. **Conversation context preservation** across agent handoffs
5. **Integration with existing models** rather than creating parallel systems

## File Structure for New Components

```
Services/
├── AIConciergeService.cs (Main orchestrator)
├── Agents/
│   ├── BaseAgent.cs
│   ├── RoomBookingAgent.cs
│   ├── RoomServiceAgent.cs
│   └── HousekeepingAgent.cs
├── ChatHub.cs (SignalR hub)
└── ConversationService.cs

Models/
├── ChatMessage.cs
├── ConversationHistory.cs
├── AgentInteraction.cs
└── MenuItem.cs

Controllers/
└── ChatController.cs

Views/
├── Chat/
│   ├── _ChatWidget.cshtml
│   └── _ChatWindow.cshtml
└── Manager/
    └── ChatAnalytics.cshtml

wwwroot/
├── js/
│   ├── chat.js
│   └── signalr-chat.js
└── css/
    └── chat.css
```

## Database Schema Extensions

### ConversationHistory Table
```sql
CREATE TABLE ConversationHistory (
    ConversationId UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId NVARCHAR(100),
    UserId NVARCHAR(100) NULL,
    StartTime DATETIME2,
    EndTime DATETIME2 NULL,
    IsActive BIT DEFAULT 1
);
```

### ChatMessage Table
```sql
CREATE TABLE ChatMessage (
    MessageId INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId UNIQUEIDENTIFIER,
    IsFromUser BIT,
    MessageText NVARCHAR(MAX),
    AgentType NVARCHAR(50),
    Timestamp DATETIME2 DEFAULT GETUTCDATE(),
    MessageMetadata NVARCHAR(MAX) -- JSON for additional data
);
```

### AgentInteraction Table
```sql
CREATE TABLE AgentInteraction (
    InteractionId INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId UNIQUEIDENTIFIER,
    AgentType NVARCHAR(50),
    Action NVARCHAR(100),
    Success BIT,
    Duration INT, -- milliseconds
    Timestamp DATETIME2 DEFAULT GETUTCDATE()
);
```

This plan ensures a comprehensive AI concierge solution that leverages the existing Contoso Hotels infrastructure while adding powerful conversational AI capabilities. Each agent will be specialized but work together seamlessly through the Semantic Kernel orchestrator.
