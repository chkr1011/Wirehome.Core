function createModalService() {
    var srv = this;

    srv.show = function (title, message) {
        $("#modal-header").html(title);
        $("#modal-body").html(message);
        $("#modal").modal("show");
    };

    srv.showInfoPopover = function () {
        $("#infoIcon").popover({
            html: true,
            title: "Wirehome.App",
            placement: "top",
            content: function () {
                return $('#infoPopoverContent').html();
            }
        });
    };

    return this;
}