var standartSelect = $(".standart-select");

var deleteProductButton = $("#deleteProduct");
$(document).ready(function () {


    $(document.body).on('click', ".clickable-row td", function () {
        if ($(this).find('input:checkbox').length < 1) {
            window.location = $(this).parent().data("href");
        }
    });

    standartSelect.on("change", function () {
        ChangeUnit($(this));
    });

    $(document.body).on('change', ".check-delete", function () {
        var checkedInputs = $(".check-delete:checked");
        ItemsChecked(deleteProductButton, checkedInputs);
    });

    deleteProductButton.on("click", function () {
        DeleteProducts();
    });

    standartSelect.trigger("change");
    new Choices(document.querySelector(".standart-select"));
    new Choices(document.querySelector(".manufacturer-select"));
});

function ChangeUnit(select) {
    var selectedStandId = select.val();
    var unitId = standartUnits[selectedStandId];
    var unit = units[unitId];
    var unitShort = unit["unitShort"];
    var quantLabel = $('label[for="Quantity"]');
    quantLabel.text(`Квант (${unitShort})`);
}

function DeleteProducts() {
    var checkedInputs = $(".check-delete:checked");
    var idArray = [];
    checkedInputs.each(function () {
        idArray.push($(this).data("id"));
    });

    $(".clickable-row td").html("<span></span>");
    $('table').addClass("loading");
    $("#deleteProduct").attr("disabled", true);
    var deleteData = JSON.stringify(idArray);
    $.ajax({
        url: "/catalog/deleteProducts",
        method: 'POST',
        contentType: "application/json; charset=utf-8",
        data: deleteData,
        success: () => {
            RefreshProductsTable();
        },
        error: () => {
            console.error("error delete products");
        }
    });
}

function RefreshProductsTable() {
    data = GetDataForRefreshTable();
    $(".clickable-row td").html("<span></span>");
    $.ajax({
        url: "/catalog/refreshProductsTable",
        method: 'GET',
        data,
        success: (html) => {
            $('table tbody').html(html);
            $('table').removeClass("loading");
        }
    });
}

function ShowHideManufacturer(element) {
    $("label[for='ManufacturerId']").parent().toggleClass("d-none");
    $("label[for='ManufacturerName']").parent().toggleClass("d-none");
}

function ChangeOrder() {
    let order = $("#selectOrder").val();
    let direction = $("#selectDirection").val();
    $(".clickable-row td").html("<span></span>");
    $.ajax({
        url: "/catalog/refreshProductsTable",
        method: 'GET',
        data: {
            propertyNameForOrder: order,
            orderDirection: direction
        },
        success: (html) => {
            $('table tbody').html(html);
            $('table').removeClass("loading");
        }
    });
}

function GetDataForRefreshTable() {
    let order = $("#selectOrder").val();
    let direction = $("#selectDirection").val();
    let search = $("#search").val();
    data = {
        propertyNameForOrder: order,
        orderDirection: direction,
        searchString: search,
    }
    return data;
}

$("#search").keypress(function (event) {
    var keycode = (event.keyCode ? event.keyCode : event.which);
    if (keycode == '13') {
        RefreshProductsTable();
    }
});

$("#search").focusout(function (event) {
    RefreshProductsTable();
})