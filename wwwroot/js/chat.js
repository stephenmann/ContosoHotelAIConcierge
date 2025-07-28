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
    }
    
    initializeSignalR() {
        try {
            // Initialize SignalR connection (will be implemented in Phase 2.2)
            console.log('SignalR connection will be initialized in Phase 2.2');
            
            // Simulate connection status for now
            setTimeout(() => {
                this.isConnected = true;
                this.updateConnectionStatus();
            }, 1000);
            
        } catch (error) {
            console.error('SignalR initialization error:', error);
            this.updateConnectionStatus(false);
        }
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
        // Simulate AI response for now (will be replaced with SignalR in Phase 2.2)
        setTimeout(() => {
            this.hideTypingIndicator();
            
            // Simulate different agent responses based on message content
            let response = this.generateSimulatedResponse(message);
            this.addMessage(response.text, 'agent', response.agent);
        }, 1500 + Math.random() * 1000); // Random delay for realism
    }
    
    generateSimulatedResponse(message) {
        const lowerMessage = message.toLowerCase();
        
        if (lowerMessage.includes('book') || lowerMessage.includes('room') || lowerMessage.includes('reservation')) {
            return {
                text: "I'd be happy to help you book a room! To get started, could you please tell me:\n\n• What dates are you looking to stay?\n• Which city would you prefer?\n• How many guests will be staying?\n\nI can then show you our available rooms with pricing and amenities.",
                agent: 'booking'
            };
        }
        
        if (lowerMessage.includes('service') || lowerMessage.includes('food') || lowerMessage.includes('dining') || lowerMessage.includes('menu')) {
            return {
                text: "I can help you with room service! Our kitchen is open 24/7 and we have a variety of options including:\n\n• Continental and American breakfast\n• Lunch and dinner entrees\n• Snacks and beverages\n• Dietary accommodations\n\nWould you like me to show you our current menu, or do you have something specific in mind?",
                agent: 'service'
            };
        }
        
        if (lowerMessage.includes('housekeeping') || lowerMessage.includes('cleaning') || lowerMessage.includes('towels') || lowerMessage.includes('maintenance')) {
            return {
                text: "I can arrange housekeeping services for you! We offer:\n\n• Daily room cleaning\n• Fresh towels and linens\n• Maintenance requests\n• Special cleaning services\n\nWhat type of housekeeping assistance do you need today?",
                agent: 'housekeeping'
            };
        }
        
        if (lowerMessage.includes('amenities') || lowerMessage.includes('facilities') || lowerMessage.includes('pool') || lowerMessage.includes('gym')) {
            return {
                text: "Contoso Hotels offers exceptional amenities including:\n\n• Fitness center and spa\n• Swimming pool and hot tub\n• Business center\n• Free Wi-Fi throughout\n• 24/7 concierge service\n• Valet parking\n\nIs there a specific amenity you'd like to know more about?",
                agent: 'general'
            };
        }
        
        // Default response
        return {
            text: "Thank you for your message! I'm here to assist you with:\n\n• Room bookings and reservations\n• Room service orders\n• Housekeeping requests\n• Hotel amenities and services\n\nHow can I help make your stay more comfortable?",
            agent: 'general'
        };
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
