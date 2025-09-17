// Custom Swagger UI JavaScript enhancements
document.addEventListener('DOMContentLoaded', function() {
    // Add notification count badge to relevant endpoints
    setTimeout(function() {
        addNotificationBadges();
        addSignalRInfo();
        enhanceExamples();
    }, 1000);
});

function addNotificationBadges() {
    const notificationEndpoints = [
        '/api/Notifications',
        '/api/InAppNotifications',
        '/api/Templates'
    ];
    
    notificationEndpoints.forEach(endpoint => {
        const elements = document.querySelectorAll(`[data-path="${endpoint}"]`);
        elements.forEach(el => {
            if (!el.querySelector('.notification-badge')) {
                const badge = document.createElement('span');
                badge.className = 'notification-badge';
                badge.textContent = 'NEW';
                el.appendChild(badge);
            }
        });
    });
}

function addSignalRInfo() {
    const infoSection = document.querySelector('.info');
    if (infoSection && !document.querySelector('.signalr-section')) {
        const signalrDiv = document.createElement('div');
        signalrDiv.className = 'signalr-section';
        signalrDiv.innerHTML = `
            <h3>🔄 Real-time SignalR Hub</h3>
            <p><strong>WebSocket Endpoint:</strong> <code>/notificationHub</code></p>
            <p><strong>Events:</strong> ReceiveNotification, UserJoined, UserLeft, NotificationRead</p>
            <p><strong>Authentication:</strong> JWT Bearer token required</p>
            <p>Connect to receive real-time notifications and manage user presence.</p>
        `;
        infoSection.appendChild(signalrDiv);
    }
}

function enhanceExamples() {
    // Add copy buttons to code examples
    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        if (!block.parentElement.querySelector('.copy-button')) {
            const copyButton = document.createElement('button');
            copyButton.textContent = 'Copy';
            copyButton.className = 'copy-button';
            copyButton.style.cssText = `
                position: absolute;
                top: 5px;
                right: 5px;
                background: #3498db;
                color: white;
                border: none;
                padding: 5px 10px;
                border-radius: 3px;
                cursor: pointer;
                font-size: 12px;
                z-index: 1000;
            `;
            
            copyButton.onclick = function() {
                navigator.clipboard.writeText(block.textContent).then(() => {
                    copyButton.textContent = 'Copied!';
                    setTimeout(() => copyButton.textContent = 'Copy', 2000);
                });
            };
            
            block.parentElement.style.position = 'relative';
            block.parentElement.appendChild(copyButton);
        }
    });
}

// Add keyboard shortcuts
document.addEventListener('keydown', function(e) {
    // Ctrl/Cmd + K to focus search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.querySelector('.filter-container input');
        if (searchInput) {
            searchInput.focus();
        }
    }
    
    // Ctrl/Cmd + E to expand all
    if ((e.ctrlKey || e.metaKey) && e.key === 'e') {
        e.preventDefault();
        const expandButtons = document.querySelectorAll('.opblock-summary');
        expandButtons.forEach(button => {
            if (!button.parentElement.classList.contains('is-open')) {
                button.click();
            }
        });
    }
    
    // Ctrl/Cmd + R to collapse all
    if ((e.ctrlKey || e.metaKey) && e.key === 'r') {
        e.preventDefault();
        const expandButtons = document.querySelectorAll('.opblock-summary');
        expandButtons.forEach(button => {
            if (button.parentElement.classList.contains('is-open')) {
                button.click();
            }
        });
    }
});

// Add helpful tooltips
function addTooltips() {
    const authButton = document.querySelector('.btn.authorize');
    if (authButton && !authButton.title) {
        authButton.title = 'Click to add JWT Bearer token for authentication';
    }
    
    const tryItButtons = document.querySelectorAll('.try-out__btn');
    tryItButtons.forEach(btn => {
        if (!btn.title) {
            btn.title = 'Test this endpoint with live data';
        }
    });
}

// Initialize tooltips after DOM updates
const observer = new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
        if (mutation.type === 'childList') {
            setTimeout(addTooltips, 100);
        }
    });
});

observer.observe(document.body, {
    childList: true,
    subtree: true
});
