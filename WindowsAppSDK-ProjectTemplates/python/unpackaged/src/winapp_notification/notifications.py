from winappsdk.microsoft.windows.appnotifications import (
    AppNotification,
    AppNotificationManager,
    AppNotificationPriority,
)


NOTIFICATION_TYPES = {
    "simple": {
        "xml": """
        <toast>
            <visual>
                <binding template="ToastGeneric">
                    <text>Hello from Python!</text>
                    <text>Windows App SDK Notification</text>
                </binding>
            </visual>
        </toast>
        """,
        "priority": None,
    },
    "image": {
        "xml": """
        <toast>
            <visual>
                <binding template="ToastGeneric">
                    <text>Rich Notification</text>
                    <text>Notification with image and buttons</text>
                    <image placement="hero" src="https://picsum.photos/364/180?image=883" />
                </binding>
            </visual>
            <actions>
                <action content="View" arguments="action=view" />
                <action content="Dismiss" arguments="action=dismiss" />
            </actions>
        </toast>
        """,
        "priority": AppNotificationPriority.HIGH,
    },
    "progress": {
        "xml": """
        <toast>
            <visual>
                <binding template="ToastGeneric">
                    <text>Processing Data</text>
                    <text>Task in progress...</text>
                    <progress value="0.6" status="60% Complete" />
                </binding>
            </visual>
        </toast>
        """,
        "priority": None,
    },
    "reminder": {
        "xml": """
        <toast scenario="reminder">
            <visual>
                <binding template="ToastGeneric">
                    <text>Python Reminder</text>
                    <text>Don't forget to check your tasks!</text>
                </binding>
            </visual>
            <actions>
                <input id="snoozeTime" type="selection" defaultInput="15">
                    <selection id="5" content="5 minutes" />
                    <selection id="15" content="15 minutes" />
                    <selection id="60" content="1 hour" />
                </input>
                <action activationType="system" arguments="snooze" content="Snooze" />
                <action activationType="system" arguments="dismiss" content="Dismiss" />
            </actions>
        </toast>
        """,
        "priority": None,
    },
}


def show_notification(notification_type="simple"):
    """Show a toast notification.
    
    Args:
        notification_type: Type of notification - "simple", "image", "progress", or "reminder"
    """
    if notification_type not in NOTIFICATION_TYPES:
        raise ValueError(f"Unknown notification type: {notification_type}")
    
    config = NOTIFICATION_TYPES[notification_type]
    notification = AppNotification(config["xml"])
    
    if config["priority"]:
        notification.priority = config["priority"]
    
    AppNotificationManager.default.show(notification)
