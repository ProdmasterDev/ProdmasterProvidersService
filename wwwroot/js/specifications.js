var deleteSpecificationButton = $("#deleteSpecification");

var addProductButton = $('button#addProduct');

var productTableBody = $('table#productTable tbody');

var startsatInput = $('input#StartsAt');
$(document).ready(function () {
    $(document.body).on('click', ".clickable-row td", function () {
        if ($(this).find('input:checkbox').length < 1) {
            window.location = $(this).parent().data("href");
        }
    });

    $(document.body).on('change', ".check-delete-spec", function () {
        var checkedInputs = $(".check-delete-spec:checked");
        ItemsChecked(deleteSpecificationButton, checkedInputs);
    });

    addProductButton.on("click", function (e) {
        e.preventDefault();
        AddProduct();
    });

    deleteSpecificationButton.on("click", function () {
        DeleteSpecifications();
    });

    startsatInput.on("change", function () {
        UpdateMinDate($(this).val());
    });

    productTableBody.on("click", ".delete-product", function (e) {
        e.preventDefault();
        DeleteProductFromTable($(this));
    });

    $(document.body).on('change', "select.specification-product-id", function () {
        UpdateProductsSelect();
    });
});

function RefreshSpecificationsTable() {
    $(".clickable-row td").html("<span></span>");
    $.ajax({
        url: "/specification/refreshSpecificationsTable",
        method: 'GET',
        success: (html) => {
            $('table tbody').html(html);
            $('table').removeClass("loading");
        }
    });
}

function DeleteSpecifications() {
    var checkedInputs = $(".check-delete-spec:checked");
    var idArray = [];
    checkedInputs.each(function () {
        idArray.push($(this).data("id"));
    });

    $(".clickable-row td").html("<span></span>");
    $('table').addClass("loading");
    $("#deleteSpecification").attr("disabled", true);
    var deleteData = JSON.stringify(idArray);
    $.ajax({
        url: "/specification/deleteSpecifications",
        method: 'POST',
        contentType: "application/json; charset=utf-8",
        data: deleteData,
        success: () => {
            RefreshSpecificationsTable();
        },
        error: () => {
            console.error("error delete specifications");
        }
    });
}

function AddProduct() {
    var availableProducts = GetAvailableProductList();
    var productTableLastIndex = productTableBody.find("tr").last().attr('data-index');
    productTableLastIndex = parseInt(productTableLastIndex);

    if (!Object.keys(availableProducts).length) {
        alert("Нет товаров для добавления");
        return;
    }
    markup = CreateRow(productTableLastIndex + 1);
    productTableBody.append(markup);
    select = productTableBody.find(`select#Products_${productTableLastIndex + 1}__Id`);
    for (var key in products) {
        select.append(`<option value="${key}" ${!availableProducts.hasOwnProperty(key) ? "disabled" : ""}>${products[key]}</option>`);
    }
    UpdateProductsSelect();
}

function GetAvailableProductList() {
    var addedProducts = [];
    $('input.specification-product-id').each(function () {
        addedProducts.push($(this).val());
    });
    $('select.specification-product-id').each(function () {
        addedProducts.push($(this).val());
    });
    var availableProducts = {};
    for (var key in products) {
        if (!addedProducts.includes(key))
            availableProducts[key] = products[key];
    }
    return availableProducts;
}

function CreateRow(index) {
    markup =
        `<tr data-index=${index}>
            <td>
                <select class="form-select standart-select specification-product-id" data-val="true" data-val-required="The Id field is required." id="Products_${index}__Id" name="Products[${index}].Id"></select>
            </td>
            <td>
                <input type="number" step="any" class="form-control" data-val="true" data-val-number="The field Price must be a number." data-val-range="Неверное значение цены" data-val-range-max="1.7976931348623157E+308" data-val-range-min="5E-324" data-val-required="Не указана цена" id="Products_${index}__Price" name="Products[${index}].Price" value="">
            </td>
            <td>
                <button class="btn-close delete-product"></button>
            </td>
        </tr>`;
    return markup;
}

function UpdateMinDate(inputValue) {
    var expiresatInput = $('input#ExpiresAt');
    var date = new Date(inputValue);
    date.setDate(date.getDate() + 7);
    expiresatInput.attr("min", date.toISOString().substring(0, 10));
}

function DeleteProductFromTable(element) {
    var select = element.parents().eq(1).find('select');
    if (select != undefined) {
        $(`select.specification-product-id option[value=${select.val()}]`).removeAttr('disabled');
    }
    element.parents().eq(1).remove();
    let productsInTable = productTableBody.find('tr');
    for (let i = 0; i < productsInTable.length; i++) {
        var index = $(productsInTable[i]).data("index");
        var curIdEl = productsInTable.find(`#Products_${index}__Id`)
        curIdEl.attr("id", `Products_${i}__Id`);
        curIdEl.attr("name", `Products[${i}].Id`);
        var curPriceEl = productsInTable.find(`#Products_${index}__Price`)
        curPriceEl.attr("id", `Products_${i}__Price`);
        curPriceEl.attr("name", `Products[${i}].Price`);
        $(productsInTable[i]).attr("data-index", i);
    }
    UpdateProductsSelect();
}

function UpdateProductsSelect() {

    selects = productTableBody.find('select.specification-product-id');
    var selectedProducts = [];
    selects.each(function () {
        selectedProducts.push($(this).val());
    });

    inputs = productTableBody.find('input.specification-product-id')
    inputs.each(function () {
        selectedProducts.push($(this).val());
    });

    $('select.specification-product-id option').removeAttr('disabled');
    selectedProducts.forEach(product => {
        var selectOptions = selects.find(`option[value=${product}]`);
        selectOptions.each(function () {
            if ($(this).parent().val() != $(this).val())
            $(this).attr('disabled', 'disabled');
        });
        //$(`select.specification-product-id option[value=${product}]`).attr('disabled', 'disabled');
    });
    //$(`select.specification-product-id option[value=${product}]`).attr('disabled', 'disabled');    
}