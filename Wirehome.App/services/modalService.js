function createModalService() {
    var srv = this;

    srv.show = function(title, message) {
        $("#modal-header").html(title);
        $("#modal-body").html(message);
        $("#modal").modal("show");
    }

    return this;
}