$(document).ready(() => {
    $(document).on('click', '.standard-detail', function () {
        $.ajax({
            method: "GET",
            url: `/standardDetail?id=${$(this).attr('sid')}`,
            success: (html) => {
                $('#standardModal .modal-content').html($(html));
            },
            error: (data) => {
                console.log(data)
            }
        })
    });

    $(document).on('submit', 'form#standartSearch', function (e) {
        e.preventDefault();

        var spinner = $('span#searchSpinner');
        spinner.removeClass('d-none');

        var searchText = $('input#standartSearch').val();

        $.ajax({
            method: "GET",
            url: `/standardAccordion?searchString=${searchText}`,
            success: (html) => {
                spinner.addClass('d-none');
                $('#accordionStandarts').html($(html));
            },
            error: (data) => {
                console.log(data)
            }
        })
    });

});