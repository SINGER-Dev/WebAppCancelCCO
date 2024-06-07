// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var ids = [];


$(document).ready(function () {

    $('#searchForm').submit(function (event) {
        $('.loaddong').css('display', 'block');
        event.preventDefault(); // Prevent normal form submission
        var formData = $(this).serialize(); // Serialize form data
        $.ajax({
            url: $(this).attr('action'), // Action URL
            type: $(this).attr('method'), // Method (POST in this case)
            data: formData,
            success: function (result) {
                $('.loaddong').css('display', 'none');
                $('#searchResults').html(result); // Update search results
                const dt = $('#example').DataTable({
                    scrollX: true,
                    "bSort": false
                });

            },
            error: function (xhr, status, error) {
                console.error(error);
            }
        });
    });

    $("#FormCancel").submit(function (e) {


        e.preventDefault();


        Swal.fire({
            title: "ยืนยันการยกเลิกใบคำขอ?",
            showCancelButton: true,
            confirmButtonText: "ยืนยัน",
            cancelButtonText: "ออก",
        }).then((result) => {
            /* Read more about isConfirmed, isDenied below */
            if (result.isConfirmed) {
                $('#btnFetch').prop("disabled", true);
                // add spinner to button
                $('#btnFetch').html(
                    ' <span class="spinner-grow spinner-grow-sm" role="status" aria-hidden="true"></span> Loading...'
                );

                var formData = $(this).serialize(); // Serialize form data
                $.ajax({
                    url: $(this).attr('action'), // Action URL
                    type: $(this).attr('method'), // Method (POST in this case)
                    data: formData,
                    success: function (result) {
                        if (result == "") {
                            Swal.fire({
                                title: "ยกเลิกรายการ!",
                                text: "ยกเลิกรายการสำเร็จ",
                                icon: "success"
                            }).then(function () {
                                // Redirect the user
                                window.location.href = "/";
                            });
                        }
                        else {
                            Swal.fire({
                                icon: "error",
                                title: "Oops...",
                                text: result
                            }).then(function () {
                                location.reload();
                            });
                        }
                    },
                    error: function (xhr, status, error) {
                        console.error(error);
                    }
                });
            }
        });



        

        

    });
});

