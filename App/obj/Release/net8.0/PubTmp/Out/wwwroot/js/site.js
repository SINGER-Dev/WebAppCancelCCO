    // Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
    // for details on configuring this project to bundle and minify static web assets.

    // Write your JavaScript code.

    var ids = [];


    $(document).ready(function () {

   
        $('.date').datepicker({ todayHighlight: true, format: 'yyyy/mm/dd', defaultDate: new Date() }).datepicker("setDate", new Date());

        $('#Remark').change(function (e) {
            if ($(this).val() == "อื่นๆ") {
                $('#Other').css("display", "block");
            }
            else {
                $('#Other').css("display", "none");
            }
        });

        $('#BtnsearchResults').click(function () {
            searchForm();
        });


        $('#searchFormApplicationHistory').submit(function (event) {

            checkSession();

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
                        pageLength: 5,
                        lengthMenu: [[5, 10, 20, -1], [5, 10, 20, 'Todos']],
                        layout: {
                            topStart: 'pageLength',
                            top: 'buttons',
                            topEnd: 'search'
                        }
                    });

                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            }); 
        });

        $("#btnFetch2").click(function (e) {

            checkSession();

            e.preventDefault();




            if ($.trim($('#Remark').val()) == "" || ($.trim($('#Remark').val()) == "อื่นๆ" && $.trim($('#Other').val()) == "")) {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "กรุณากรอกข้อมูลให้ครบถ้วน"
                })
            }
            else {

                Swal.fire({
                    title: "ยืนยันการยกเลิกใบคำขอ " + $('#ApplicationCode').val(),
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ออก",
                }).then((result) => {
                    /* Read more about isConfirmed, isDenied below */
                    if (result.isConfirmed) {
                        $('#btnFetch2').prop("disabled", true);
                        // add spinner to button
                        $('#btnFetch2').html(
                            ' <span class="spinner-grow spinner-grow-sm" role="status" aria-hidden="true"></span> Loading...'
                        );

                        var formData = $("#FormCancel").serialize(); // Serialize form data
                        $.ajax({
                            url: "/Home/UpdateDataCancelCLOSED", // Action URL
                            type: "POST", // Method (POST in this case)
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
            }
        });

        $("#btnFetch").click(function (e) {

            checkSession();

            e.preventDefault();

            if ($.trim($('#Remark').val()) == "" || ($.trim($('#Remark').val()) == "อื่นๆ" && $.trim($('#Other').val()) == "")) {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "กรุณากรอกข้อมูลให้ครบถ้วน"
                })
            }
            else {

                Swal.fire({
                    title: "ยืนยันการยกเลิกใบคำขอ " + $('#ApplicationCode').val(),
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

                        var formData = $("#FormCancel").serialize(); // Serialize form data
                        $.ajax({
                            url: "/Home/UpdateDataCancel", // Action URL
                            type: "POST", // Method (POST in this case)
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
            }
        });

    

        $("#btnLogin").click(function () {
            $('#btnLogin').prop("disabled", true);
            $('#btnLogin').html(
                ' <span class="spinner-grow spinner-grow-sm" role="status" aria-hidden="true"></span> Loading...'
            );


            var data = {
                user_id: $('#user_id').val(),
                password: $('#password').val()
            };

            $.ajax({
                url: "./Login/Login", // Action URL
                type: "POST", // Method (POST in this case)
                contentType: 'application/json',
                data: JSON.stringify(data),
                success: function (result) {
                    $('#btnLogin').prop("disabled", false);
                
                    if (result.statusCode == "SUCCESS") {
                        let timerInterval;
                        Swal.fire({
                            title: "กรุณารอสักครู่!",
                            html: "ระบบกำลังนำพาไปหน้าหลัก ภายในอีก <b></b> วินาที.",
                            timer: 500,
                            timerProgressBar: true,
                            didOpen: () => {
                                Swal.showLoading();
                                const timer = Swal.getPopup().querySelector("b");
                                timerInterval = setInterval(() => {
                                    timer.textContent = `${Swal.getTimerLeft()}`;
                                }, 100);
                            },
                            willClose: () => {
                                clearInterval(timerInterval);
                            }
                        }).then((result) => {
                            /* Read more about handling dismissals below */
                            if (result.dismiss === Swal.DismissReason.timer) {
                                window.location.href = "/";
                            }
                        });
                    }
                    else {
                        Swal.fire({
                            icon: "error",
                            title: "Oops...",
                            text: "UserName or Password is incorrect"
                        });
                    }

                    console.log(result);
                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            });
        });

    });

    function searchForm() {

        checkSession();


        $('.loaddong').css('display', 'block');
        event.preventDefault(); // Prevent normal form submission
        var formData = $('#searchForm').serialize(); // Serialize form data
        $.ajax({
            url: $('#searchForm').attr('action'), // Action URL
            type: $('#searchForm').attr('method'), // Method (POST in this case)
            data: formData,
            success: function (result) {
                $('.loaddong').css('display', 'none');
                $('#searchResults').html(result); // Update search results

                $(".C100StatusClosed").click(function () {

                    var data = {
                        ApplicationCode: $(this).data("applicationcode")
                    };

                    Swal.fire({
                        title: "ยืนยันการเปลี่ยนสถานะรายการ?",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ออก",
                    }).then((result) => {
                        /* Read more about isConfirmed, isDenied below */
                        if (result.isConfirmed) {

                            var formData = $(this).serialize(); // Serialize form data
                            $.ajax({
                                url: "./Home/GetStatusClosedSGFinance", // Action URL
                                type: "POST", // Method (POST in this case)
                                contentType: 'application/json',
                                data: JSON.stringify(data),
                                success: function (result) {
                                    if (result.message == "PASS") {
                                        Swal.fire({
                                            title: "เปลี่ยนสถานะรายการ!",
                                            text: "เปลี่ยนสถานะรายการสำเร็จ",
                                            icon: "success"
                                        });
                                    }
                                    else {
                                        Swal.fire({
                                            icon: "error",
                                            title: "Oops...",
                                            text: result
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

                $(".GenEsignature").click(function () {

                    var data = {
                        ApplicationCode: $(this).data("applicationcode")
                    };

                    Swal.fire({
                        title: "ยืนยันการส่งลิงค์ลงนามอีกครั้ง?",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ออก",
                    }).then((result) => {
                        /* Read more about isConfirmed, isDenied below */
                        if (result.isConfirmed) {

                            var formData = $(this).serialize(); // Serialize form data
                            $.ajax({
                                url: "./Home/GenEsignature", // Action URL
                                type: "POST", // Method (POST in this case)
                                contentType: 'application/json',
                                data: JSON.stringify(data),
                                success: function (result) {
                                    if (result.message == "Success.") {
                                        Swal.fire({
                                            title: "ส่งลิงคิ์ลงนาม!",
                                            text: "ส่งลิงค์ลงนามเสำเร็จ",
                                            icon: "success"
                                        });
                                    }
                                    else {
                                        Swal.fire({
                                            icon: "error",
                                            title: "Oops...",
                                            text: result
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

                $(".GetAddTNewSalesNewSGFinance").click(function () {

                    var data = {
                        ApplicationCode: $(this).data("applicationcode")
                    };

                    Swal.fire({
                        title: "ยืนยันรายงานขายอีกครั้ง?",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ออก",
                    }).then((result) => {
                        /* Read more about isConfirmed, isDenied below */
                        if (result.isConfirmed) {

                            var formData = $(this).serialize(); // Serialize form data
                            $.ajax({
                                url: "./Home/GetAddTNewSalesNewSGFinance", // Action URL
                                type: "POST", // Method (POST in this case)
                                contentType: 'application/json',
                                data: JSON.stringify(data),
                                success: function (result) {
                                    if (result.message == "PASS") {
                                        Swal.fire({
                                            title: "ยืนยันรายงานขาย!",
                                            text: "ยืนยันรายงานขายสำเร็จ",
                                            icon: "success"
                                        });
                                    }
                                    else {
                                        Swal.fire({
                                            icon: "error",
                                            title: "Oops...",
                                            text: result
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

                $(".RegisIMEI").click(function () {

                    var data = {
                        ApplicationCode: $(this).data("applicationcode")
                    };

                    Swal.fire({
                        title: "ยืนยันลงทะเบียนเครื่อง?",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ออก",
                    }).then((result) => {
                        /* Read more about isConfirmed, isDenied below */
                        if (result.isConfirmed) {

                            var formData = $(this).serialize(); // Serialize form data
                            $.ajax({
                                url: "./Home/RegisIMEI", // Action URL
                                type: "POST", // Method (POST in this case)
                                contentType: 'application/json',
                                data: JSON.stringify(data),
                                success: function (result) {
                                    if (result.statusCode == "PASS") {
                                        Swal.fire({
                                            title: "ลงทะเบียนเครื่อง!",
                                            text: "ลงทะเบียนเครื่องสำเร็จ",
                                            icon: "success"
                                        });
                                    }
                                    else {
                                        Swal.fire({
                                            icon: "error",
                                            title: "Oops...",
                                            text: result.statusCode.message
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


                const dt = $('#example').DataTable({
                    scrollX: true,
                    pageLength: 5,
                    lengthMenu: [[5, 10, 20, -1], [5, 10, 20, 'Todos']],
                    layout: {
                        topStart: 'pageLength',
                        top: 'buttons',
                        topEnd: 'search'
                    }
                });
            },
            error: function (xhr, status, error) {
                console.error(error);
            }
        });
    }

    function checkSession() {
        $.ajax({
            url: '/checksession',
            type: 'GET',
            success: function (data) {
                // Session valid, do nothing
            },
            error: function (xhr) {
                if (xhr.status === 401) {
                    // Session expired, redirect to login page
                    window.location.href = '/Login';
                }
            }
        });
    }