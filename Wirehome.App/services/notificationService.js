function createNotificationService(apiService) {
    var srv = this;

    srv.delete = function (notificationUid) {
        apiService.executeDelete("/api/v1/notifications/" + notificationUid);
    };

    return this;
}