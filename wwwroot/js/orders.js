var startsatInput = $('input#StartsAt');
$(document).ready(function () {
    $(document.body).on('click', ".clickable-row td", function () {
        window.location = $(this).parent().data("href");
    });

    if ($("#confirmInput").val() == $("#declineRadio").val()) {
        $("#declineRadio").click();
    }
    else {
        $("#confirmRadio").click();
    }
});

function ConfirmOrder() {
    var value = $("#confirmRadio").val();
    if (!$("#declineNoteBlock").hasClass("d-none"))
        $("#declineNoteBlock").addClass("d-none");
    $("#confirmInput").val(value);
    $("#confirmOrDeclineBtn").prop("value", "Подтверждаю");
}

function DeclineOrder() {
    var value = $("#declineRadio").val();
    if ($("#declineNoteBlock").hasClass("d-none"))
        $("#declineNoteBlock").removeClass("d-none")
    $("#confirmInput").val(value);
    $("#confirmOrDeclineBtn").prop("value", "Отклоняю");
}