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
    RefreshProductTable(value);
}

function DeclineOrder() {
    var value = $("#declineRadio").val();
    if ($("#declineNoteBlock").hasClass("d-none"))
        $("#declineNoteBlock").removeClass("d-none")
    $("#confirmInput").val(value);
    $("#confirmOrDeclineBtn").prop("value", "Отклоняю");
    RefreshProductTable(value);
}

function EditOrder() {
    var value = $("#editRadio").val();
    if ($("#declineNoteBlock").hasClass("d-none"))
        $("#declineNoteBlock").removeClass("d-none")
    $("#confirmInput").val(value);
    $("#confirmOrDeclineBtn").prop("value", "Изменяю");
    RefreshProductTable(value);
}

function RefreshProductTable() {
    var id = $("#entityId").val();
    orderState = $("#confirmInput").val();
    var data = {
        id: id,
        orderState: orderState
    }
    console.log(data);
    var url = "/order/RefreshOrderProductPart";

    $.ajax({
        url: url,
        data: data,
        method: 'POST',
        success: (html) => {
            console.log(html)
            $('table tbody').html(html);
            $('table').removeClass("loading");
        },
        error: (msg) => {
            console.log(msg)
        }
    });
}