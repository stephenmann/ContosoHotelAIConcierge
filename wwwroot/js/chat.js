/**
 * Contoso Hotels AI Concierge Chat Widget
 * Handles chat UI interactions, message display, and SignalR communication
 */

class ContosoHotelsChatWidget {
    constructor() {
        this.isOpen = false;
        this.isConnected = false;
        this.conversationId = null;
        this.connection = null;
        this.messageHistory = [];
        
        // Get saved state from localStorage
        this.loadState();
        
        // Initialize DOM elements
        this.initializeElements();
        
        // Bind event listeners
        this.bindEvents();
        
        // Initialize SignalR connection
        this.initializeSignalR();
        
        // Auto-resize input textarea
        this.initializeAutoResize();
        
        console.log('Contoso Hotels Chat Widget initialized');
    }
    
    initializeElements() {
        this.chatButton = document.getElementById('chatButton');
        this.chatWindow = document.getElementById('chatWindow');
        this.chatClose = document.getElementById('chatClose');
        this.chatMessages = document.getElementById('chatMessages');
        this.chatInput = document.getElementById('chatInput');
        this.chatSendBtn = document.getElementById('chatSendBtn');
        this.typingIndicator = document.getElementById('typingIndicator');
        this.agentStatusText = document.getElementById('agentStatusText');
        
        // Apply saved state
        if (this.isOpen) {
            this.openChat();
        }
    }
    
    bindEvents() {
        // Chat button click
        this.chatButton.addEventListener('click', () => {
            this.toggleChat();
        });
        
        // Close button click
        this.chatClose.addEventListener('click', () => {
            this.closeChat();
        });
        
        // Send button click
        this.chatSendBtn.addEventListener('click', () => {
            this.sendMessage();
        });
        
        // Input field events
        this.chatInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
        
        this.chatInput.addEventListener('input', () => {
            this.updateSendButton();
        });
        
        // Quick action buttons
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('quick-action')) {
                const message = e.target.getAttribute('data-message');
                if (message) {
                    this.chatInput.value = message;
                    this.sendMessage();
                }
            }
        });
        
        // Close chat when clicking outside
        document.addEventListener('click', (e) => {
            if (this.isOpen && 
                !this.chatWindow.contains(e.target) && 
                !this.chatButton.contains(e.target)) {
                // Optional: uncomment to close on outside click
                // this.closeChat();
            }
        });
        
        // Handle window resize
        window.addEventListener('resize', () => {
            this.adjustChatPosition();
        });

        // Handle page unload - cleanup SignalR connection
        window.addEventListener('beforeunload', () => {
            this.cleanup();
        });
    }
    
    initializeSignalR() {
        try {
            // Create SignalR connection to ChatHub
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/chathub")
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupSignalREventHandlers();

            // Start the connection
            this.startConnection();
            
        } catch (error) {
            console.error('SignalR initialization error:', error);
            this.updateConnectionStatus(false);
        }
    }

    setupSignalREventHandlers() {
        // Connection established
        this.connection.on("ConnectionEstablished", (data) => {
            console.log('Connected to chat hub:', data);
            this.isConnected = true;
            this.updateConnectionStatus(true);
            
            // Auto-join or create conversation if needed
            this.ensureConversation();
        });

        // Agent message received
        this.connection.on("AgentMessage", (data) => {
            console.log('Agent message received:', data);
            this.hideTypingIndicator();
            this.addMessage(data.Message, 'agent', data.AgentType);
            
            // Save to database
            this.saveMessageToDatabase(data.ConversationId, data.Message, false, data.AgentType);
        });

        // Message received confirmation
        this.connection.on("MessageReceived", (data) => {
            console.log('Message received confirmation:', data);
        });

        // Agent typing indicator
        this.connection.on("AgentTyping", (data) => {
            if (data.IsTyping) {
                this.showTypingIndicator();
            } else {
                this.hideTypingIndicator();
            }
        });

        // Conversation joined
        this.connection.on("ConversationJoined", (data) => {
            console.log('Joined conversation:', data);
            this.conversationId = data.ConversationId;
            this.saveState();
            
            // Load conversation history
            this.loadConversationHistory();
        });

        // Message error
        this.connection.on("MessageError", (data) => {
            console.error('Message error:', data);
            this.addMessage('Sorry, there was an error processing your message. Please try again.', 'agent', 'general');
        });

        // Connection events
        this.connection.onreconnecting((error) => {
            console.warn('SignalR reconnecting:', error);
            this.updateConnectionStatus(false);
            this.agentStatusText.textContent = 'Reconnecting...';
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected:', connectionId);
            this.updateConnectionStatus(true);
            
            // Rejoin conversation if we had one
            if (this.conversationId) {
                this.connection.invoke("JoinConversation", this.conversationId)
                    .catch(err => console.error('Error rejoining conversation:', err));
            }
        });

        this.connection.onclose((error) => {
            console.error('SignalR connection closed:', error);
            this.updateConnectionStatus(false);
            this.agentStatusText.textContent = 'Disconnected';
            
            // Attempt manual reconnection after a delay
            setTimeout(() => {
                if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                    this.startConnection();
                }
            }, 5000);
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            console.log("SignalR Connected successfully");
        } catch (err) {
            console.error("SignalR Connection failed:", err);
            this.updateConnectionStatus(false);
            
            // Retry connection after delay
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    async ensureConversation() {
        if (!this.conversationId) {
            try {
                // Create new conversation via API
                const response = await fetch('/api/chat/conversations', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        sessionId: this.generateSessionId(),
                        userId: null // Anonymous for now
                    })
                });

                if (response.ok) {
                    const data = await response.json();
                    this.conversationId = data.conversationId;
                    this.saveState();
                    
                    // Join the conversation via SignalR
                    await this.connection.invoke("JoinConversation", this.conversationId);
                } else {
                    throw new Error('Failed to create conversation');
                }
            } catch (error) {
                console.error('Error creating conversation:', error);
            }
        } else {
            // Join existing conversation
            try {
                await this.connection.invoke("JoinConversation", this.conversationId);
            } catch (error) {
                console.error('Error joining conversation:', error);
                // Reset conversation ID and try again
                this.conversationId = null;
                this.ensureConversation();
            }
        }
    }

    generateSessionId() {
        // Generate a unique session ID for this browser session
        let sessionId = sessionStorage.getItem('contoso-session-id');
        if (!sessionId) {
            sessionId = 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            sessionStorage.setItem('contoso-session-id', sessionId);
        }
        return sessionId;
    }

    async loadConversationHistory() {
        if (!this.conversationId) return;

        try {
            const response = await fetch(`/api/chat/conversations/${this.conversationId}/messages?limit=50`);
            
            if (response.ok) {
                const data = await response.json();
                
                // Clear welcome message and existing messages
                const welcomeMessage = this.chatMessages.querySelector('.welcome-message');
                if (welcomeMessage && data.Messages.length > 0) {
                    welcomeMessage.remove();
                }
                
                // Clear existing message history display (not the stored history)
                const existingMessages = this.chatMessages.querySelectorAll('.message');
                existingMessages.forEach(msg => msg.remove());
                
                // Display historical messages
                data.Messages.forEach(msg => {
                    this.displayHistoricalMessage(msg);
                });
                
                // Update message history array
                this.messageHistory = data.Messages.map(msg => ({
                    text: msg.MessageText,
                    sender: msg.IsFromUser ? 'user' : 'agent',
                    agentType: msg.AgentType,
                    timestamp: new Date(msg.Timestamp)
                }));
                
                this.scrollToBottom();
            }
        } catch (error) {
            console.warn('Could not load conversation history:', error);
        }
    }

    displayHistoricalMessage(msg) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${msg.IsFromUser ? 'user' : 'agent'}`;
        
        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'message-bubble';
        
        // Add agent tag for agent messages
        if (!msg.IsFromUser && msg.AgentType) {
            const agentTag = document.createElement('div');
            agentTag.className = `agent-tag ${msg.AgentType}`;
            agentTag.textContent = this.getAgentLabel(msg.AgentType);
            bubbleDiv.appendChild(agentTag);
        }
        
        // Add message text with line breaks
        const messageText = document.createElement('div');
        messageText.innerHTML = msg.MessageText.replace(/\n/g, '<br>');
        bubbleDiv.appendChild(messageText);
        
        messageDiv.appendChild(bubbleDiv);
        
        // Add timestamp
        const timeDiv = document.createElement('div');
        timeDiv.className = 'message-time';
        const timestamp = new Date(msg.Timestamp);
        timeDiv.textContent = timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        messageDiv.appendChild(timeDiv);
        
        this.chatMessages.appendChild(messageDiv);
    }
    
    initializeAutoResize() {
        this.chatInput.addEventListener('input', () => {
            this.chatInput.style.height = 'auto';
            this.chatInput.style.height = Math.min(this.chatInput.scrollHeight, 80) + 'px';
        });
    }
    
    toggleChat() {
        if (this.isOpen) {
            this.closeChat();
        } else {
            this.openChat();
        }
    }
    
    openChat() {
        this.isOpen = true;
        this.chatWindow.classList.add('show');
        this.chatButton.classList.add('active');
        this.chatButton.innerHTML = '<i class="fas fa-times"></i>';
        this.chatInput.focus();
        this.saveState();
        this.scrollToBottom();
        
        // Track chat opened event
        this.trackEvent('chat_opened');
    }
    
    closeChat() {
        this.isOpen = false;
        this.chatWindow.classList.remove('show');
        this.chatButton.classList.remove('active');
        this.chatButton.innerHTML = '<i class="fas fa-comments"></i>';
        this.saveState();
        
        // Track chat closed event
        this.trackEvent('chat_closed');
    }
    
    sendMessage() {
        const message = this.chatInput.value.trim();
        if (!message || !this.isConnected) {
            return;
        }
        
        // Add user message to chat
        this.addMessage(message, 'user');
        
        // Clear input
        this.chatInput.value = '';
        this.chatInput.style.height = 'auto';
        this.updateSendButton();
        
        // Show typing indicator
        this.showTypingIndicator();
        
        // Send message via SignalR (will be implemented in Phase 2.2)
        this.sendToAgent(message);
        
        // Track message sent event
        this.trackEvent('message_sent', { message_length: message.length });
    }
    
    sendToAgent(message) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
            console.error('SignalR connection not available');
            this.hideTypingIndicator();
            this.addMessage('Connection error. Please check your internet connection and try again.', 'agent', 'general');
            return;
        }

        if (!this.conversationId) {
            console.error('No conversation ID available');
            this.hideTypingIndicator();
            this.addMessage('Session error. Please refresh the page and try again.', 'agent', 'general');
            return;
        }

        // Send message via SignalR
        this.connection.invoke("SendMessage", this.conversationId, message)
            .then(() => {
                console.log('Message sent successfully');
                // Save user message to database
                this.saveMessageToDatabase(this.conversationId, message, true);
            })
            .catch(err => {
                console.error('Error sending message:', err);
                this.hideTypingIndicator();
                this.addMessage('Failed to send message. Please try again.', 'agent', 'general');
            });
    }

    async saveMessageToDatabase(conversationId, messageText, isFromUser, agentType = null) {
        try {
            const response = await fetch(`/api/chat/conversations/${conversationId}/messages`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    isFromUser: isFromUser,
                    messageText: messageText,
                    agentType: agentType,
                    containsSensitiveData: false,
                    messageMetadata: null
                })
            });

            if (!response.ok) {
                console.warn('Failed to save message to database');
            }
        } catch (error) {
            console.warn('Error saving message to database:', error);
        }
    }
    
    addMessage(text, sender, agentType = null) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${sender}`;
        
        const bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'message-bubble';
        
        // Add agent tag for agent messages
        if (sender === 'agent' && agentType) {
            const agentTag = document.createElement('div');
            agentTag.className = `agent-tag ${agentType}`;
            agentTag.textContent = this.getAgentLabel(agentType);
            bubbleDiv.appendChild(agentTag);
        }
        
        // Add message text with line breaks
        const messageText = document.createElement('div');
        messageText.innerHTML = text.replace(/\n/g, '<br>');
        bubbleDiv.appendChild(messageText);
        
        messageDiv.appendChild(bubbleDiv);
        
        // Add timestamp
        const timeDiv = document.createElement('div');
        timeDiv.className = 'message-time';
        timeDiv.textContent = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        messageDiv.appendChild(timeDiv);
        
        // Remove welcome message if this is the first user message
        if (sender === 'user' && this.messageHistory.length === 0) {
            const welcomeMessage = this.chatMessages.querySelector('.welcome-message');
            if (welcomeMessage) {
                welcomeMessage.remove();
            }
        }
        
        this.chatMessages.appendChild(messageDiv);
        this.messageHistory.push({ text, sender, agentType, timestamp: new Date() });
        
        this.scrollToBottom();
    }
    
    getAgentLabel(agentType) {
        const labels = {
            'booking': 'Booking',
            'service': 'Room Service',
            'housekeeping': 'Housekeeping',
            'general': 'Concierge'
        };
        return labels[agentType] || 'Assistant';
    }
    
    showTypingIndicator() {
        this.typingIndicator.classList.add('show');
        this.scrollToBottom();
    }
    
    hideTypingIndicator() {
        this.typingIndicator.classList.remove('show');
    }
    
    updateSendButton() {
        const hasText = this.chatInput.value.trim().length > 0;
        this.chatSendBtn.disabled = !hasText || !this.isConnected;
    }
    
    updateConnectionStatus(connected = this.isConnected) {
        this.isConnected = connected;
        
        if (connected) {
            this.agentStatusText.textContent = 'Online & Ready';
            this.agentStatusText.previousElementSibling.style.background = '#10b981';
        } else {
            this.agentStatusText.textContent = 'Connecting...';
            this.agentStatusText.previousElementSibling.style.background = '#f59e0b';
        }
        
        this.updateSendButton();
    }
    
    scrollToBottom() {
        setTimeout(() => {
            this.chatMessages.scrollTop = this.chatMessages.scrollHeight;
        }, 100);
    }
    
    adjustChatPosition() {
        // Handle responsive positioning if needed
        const isMobile = window.innerWidth <= 768;
        // Add any mobile-specific adjustments here
    }
    
    saveState() {
        try {
            const state = {
                isOpen: this.isOpen,
                conversationId: this.conversationId,
                timestamp: Date.now()
            };
            localStorage.setItem('contoso-chat-state', JSON.stringify(state));
        } catch (error) {
            console.warn('Could not save chat state to localStorage:', error);
        }
    }
    
    loadState() {
        try {
            const saved = localStorage.getItem('contoso-chat-state');
            if (saved) {
                const state = JSON.parse(saved);
                
                // Only restore state if it's recent (within 24 hours)
                const isRecent = (Date.now() - state.timestamp) < (24 * 60 * 60 * 1000);
                
                if (isRecent) {
                    this.isOpen = state.isOpen || false;
                    this.conversationId = state.conversationId;
                }
            }
        } catch (error) {
            console.warn('Could not load chat state from localStorage:', error);
        }
    }
    
    trackEvent(eventName, properties = {}) {
        // Track usage analytics (implement with your preferred analytics service)
        console.log('Chat Event:', eventName, properties);
        
        // Example: Google Analytics tracking
        // if (typeof gtag !== 'undefined') {
        //     gtag('event', eventName, {
        //         event_category: 'ai_chat',
        //         ...properties
        //     });
        // }
    }
    
    // Public methods for external integration
    sendQuickMessage(message) {
        if (typeof message === 'string' && message.trim()) {
            this.chatInput.value = message;
            if (!this.isOpen) {
                this.openChat();
            }
            setTimeout(() => this.sendMessage(), 500);
        }
    }
    
    openChatWithMessage(message) {
        this.openChat();
        if (message) {
            setTimeout(() => this.sendQuickMessage(message), 600);
        }
    }

    // Cleanup method for proper resource disposal
    cleanup() {
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            try {
                // Leave conversation before disconnecting
                if (this.conversationId) {
                    this.connection.invoke("LeaveConversation", this.conversationId);
                }
                
                // Stop the connection
                this.connection.stop();
            } catch (error) {
                console.warn('Error during cleanup:', error);
            }
        }
    }
}

// Expose chat widget globally for external access
window.ContosoHotelsChatWidget = ContosoHotelsChatWidget;

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        if (!window.contosoChat) {
            window.contosoChat = new ContosoHotelsChatWidget();
        }
    });
} else {
    // DOM is already ready
    if (!window.contosoChat) {
        window.contosoChat = new ContosoHotelsChatWidget();
    }
}
