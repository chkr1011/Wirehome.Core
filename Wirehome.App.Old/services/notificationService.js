function createNotificationService(apiService) {
    var srv = this;

    srv.delete = function (notificationId) {
        apiService.deleteNotification(notificationId);
    };

    return this;
}