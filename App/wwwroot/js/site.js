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

        // ผูกที่ submit ของฟอร์ม ไม่ใช่ที่ปุ่ม — กด Enter ในช่องค้นหาจึงใช้งานได้
        // (เดิมกด Enter จะ submit แบบปกติ แล้วเบราว์เซอร์เด้งไปหน้าที่มีแต่ตารางดิบ ๆ)
        $('#searchForm').on('submit', function (e) {
            e.preventDefault();
            searchForm(1, currentPageSize());
        });

        $('#BtnClearFilters').on('click', function () {
            $('#searchForm').find('input[type="text"]').val('');
            $('#status, #StatusRegis').val('');
            $('#loanTypeCate').val('LOCKPHONE');
            $('.date').datepicker('setDate', new Date());
            $('#ApplicationCode').trigger('focus');
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
                        buttons: ['excel'],
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

        $("#btnBypassCustomer").click(function (e) {

            checkSession();

            e.preventDefault();

            if ($.trim($('#IdCard').val()) == "" || $.trim($('#Remark').val()) == "") {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "กรุณากรอกข้อมูลให้ครบถ้วน"
                })
            }
            else {

                Swal.fire({
                    title: "ยืนยันการ By Pass Customer " + $('#IdCard').val(),
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ออก",
                }).then((result) => {
                    /* Read more about isConfirmed, isDenied below */
                    if (result.isConfirmed) {


                        var formData = $("#FormBypassCustomer").serialize(); // Serialize form data
                        $.ajax({
                            url: "/Home/PostBypassCustomer", // Action URL
                            type: "POST", // Method (POST in this case)
                            data: formData,
                            success: function (result) {
                                if (result.status == "Success") {
                                    Swal.fire({
                                        title: "By Pass Customer",
                                        text: result.message,
                                        icon: "success"
                                    }).then(function () {
                                        // Redirect the user
                                        window.location.href = "/";
                                    });
                                }
                                else if (result.status == "BadRequest") {
                                    Swal.fire({
                                        title: "By Pass Customer",
                                        text: result.message,
                                        icon: "error"
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

        $("#btnBypassIMEI").click(function (e) {

            checkSession();

            e.preventDefault();

            if ($.trim($('#Imei').val()) == "" || $.trim($('#Remark').val()) == "") {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "กรุณากรอกข้อมูลให้ครบถ้วน"
                })
            }
            else {

                Swal.fire({
                    title: "ยืนยันการ By Pass IMEI " + $('#Imei').val(),
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ออก",
                }).then((result) => {
                    /* Read more about isConfirmed, isDenied below */
                    if (result.isConfirmed) {


                        var formData = $("#FormBypassIMEI").serialize(); // Serialize form data
                        $.ajax({
                            url: "/Home/PostBypassIMEI", // Action URL
                            type: "POST", // Method (POST in this case)
                            data: formData,
                            success: function (result) {
                                if (result.status == "Success") {
                                    Swal.fire({
                                        title: "By Pass IMEI",
                                        text: result.message,
                                        icon: "success"
                                    }).then(function () {
                                        // Redirect the user
                                        window.location.href = "/";
                                    });
                                }
                                else if (result.status == "BadRequest") {
                                    Swal.fire({
                                        title: "By Pass IMEI",
                                        text: result.message,
                                        icon: "error"
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

        $("#btnChangeIMEI").click(function (e) {

            checkSession();

            e.preventDefault();

            if ($.trim($('#accNo').val()) == "" || $.trim($('#OldImei').val()) == "" || $.trim($('#NewImei').val()) == "") {
                Swal.fire({
                    icon: "error",
                    title: "Oops...",
                    text: "กรุณากรอกข้อมูลให้ครบถ้วน"
                })
            }
            else {

                Swal.fire({
                    title: "ยืนยันการเปลี่ยน IMEI " ,
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ออก",
                }).then((result) => {
                    /* Read more about isConfirmed, isDenied below */
                    if (result.isConfirmed) {


                        var formData = $("#FormChangeIMEI").serialize(); // Serialize form data
                        $.ajax({
                            url: "/Home/PostChangeIMEI", // Action URL
                            type: "POST", // Method (POST in this case)
                            data: formData,
                            success: function (result) {
                                if (result.status == "Success") {
                                    Swal.fire({
                                        title: "Change IMEI",
                                        text: result.message,
                                        icon: "success"
                                    }).then(function () {
                                        // Redirect the user
                                        window.location.href = "/";
                                    });
                                }
                                else if (result.status == "BadRequest") {
                                    Swal.fire({
                                        title: "Change IMEI",
                                        text: result.message,
                                        icon: "error"
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

    // ---------- ตัวช่วยกลางสำหรับปุ่มที่ยิงไปแก้ข้อมูลปลายทาง ----------
    //
    // เดิมแต่ละปุ่มเขียนโค้ดของตัวเอง และตอนล้มเหลวส่ง object เข้า SweetAlert ตรง ๆ
    // (`text: result`) หน้าจอจึงขึ้น "[object Object]" ส่วน error handler ก็แค่ console.error
    // ทำให้ผู้ใช้แยกไม่ออกว่างานซ่อมสำเร็จหรือไม่
    //
    // runFixAction ทำให้ทุกปุ่มมีพฤติกรรมเดียวกัน: ถามยืนยัน → ปิดปุ่มกันกดซ้ำ →
    // ยิง → แสดงผลจริงจากฝั่ง server → รีเฟรชตารางเมื่อสำเร็จ
    function runFixAction(opts) {
        var $icon = opts.$icon;
        if ($icon && $icon.data('busy')) { return; }

        var code = (opts.body && opts.body.ApplicationCode) || '';

        slipModal({
            kind: 'ask', code: code,
            operation: opts.confirmTitle,
            badge: 'รอยืนยัน',
            message: opts.confirmDetail || 'ยืนยันเพื่อทำรายการนี้'
        }, {
            showCancelButton: true,
            confirmButtonText: opts.confirmButtonText || 'ยืนยันทำรายการ',
            cancelButtonText: 'ยกเลิก',
            reverseButtons: true,
            focusCancel: true
        }).then(function (choice) {
            if (!choice.isConfirmed) { return; }

            if ($icon) { $icon.data('busy', true).addClass('is-busy'); }

            $.ajax({
                url: opts.url,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(opts.body),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).done(function (res) {
                if (res && res.ok) {
                    showActionSuccess(opts, code, res);
                } else {
                    showActionResult('warning', opts, code, res);
                }
            }).fail(function (xhr) {
                if (xhr.status === 401) { window.location.href = '/Login'; return; }
                showActionResult(xhr.status === 403 ? 'blocked' : 'error', opts, code, xhr.responseJSON, xhr.status);
            }).always(function () {
                if ($icon) { $icon.data('busy', false).removeClass('is-busy'); }
            });
        });
    }

    // ---------- ใบสรุปรายการ ----------
    // ออกแบบให้อ่านเหมือนเอกสารของงาน: หัวบอกว่าทำอะไรและผลเป็นอย่างไร
    // กลางใบคือเลขที่ใบคำขอตัวใหญ่ (พระเอกของงาน CCO) แล้วจึงเป็นผลลัพธ์
    // ท้ายใบเป็นผู้ทำและเวลา แบบเดียวกับใบสรุปรายการทั่วไป

    function slipHtml(o) {
        var row = rowsByCode[o.code] || {};
        var who = $('.results-wrap').data('actor') || '';
        var stamp = new Date().toLocaleString('th-TH', {
            day: 'numeric', month: 'short', year: '2-digit',
            hour: '2-digit', minute: '2-digit', hour12: false
        });

        var sub = [row.customerName, row.loanTypeCate].filter(Boolean).join(' · ');

        var html = '' +
        '<div class="slip slip-' + o.kind + '">' +
            '<div class="slip-head">' +
                '<span class="slip-op">' + esc(o.operation) + '</span>' +
                '<span class="slip-stamp">' + esc(o.badge) + '</span>' +
            '</div>' +

            '<div class="slip-body">' +
                (o.code ? '<div class="slip-docno">' + esc(o.code) + '</div>' : '') +
                (sub ? '<div class="slip-docsub">' + esc(sub) + '</div>' : '') +
                '<div class="slip-cut"></div>' +
                '<div class="slip-result">' + esc(o.message) + '</div>' +
                (o.hint ? '<div class="slip-next">' + esc(o.hint) + '</div>' : '') +
            '</div>' +

            '<div class="slip-foot">' +
                (who ? '<div><span>ทำโดย</span> ' + esc(who) + '</div>' : '<div></div>') +
                '<div><span>เวลา</span> ' + esc(stamp) + '</div>' +
            '</div>' +
        '</div>';

        if (o.detail) {
            html += '<details class="slip-detail">' +
                    '<summary>ข้อมูลสำหรับแจ้งทีมผู้ดูแล</summary>' +
                    '<pre id="ccoDetailText">' + esc(o.detail) + '</pre>' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary" id="ccoCopy">คัดลอกข้อมูล</button>' +
                    '</details>';
        }
        return html;
    }

    function slipModal(o, buttons) {
        return Swal.fire($.extend({
            html: slipHtml(o),
            buttonsStyling: false,
            width: '35rem',
            customClass: {
                popup: 'slip-popup',
                htmlContainer: 'slip-container',
                actions: 'slip-actions',
                confirmButton: 'btn btn-primary',
                cancelButton: 'btn btn-outline-secondary'
            },
            didOpen: function () {
                $('#ccoCopy').on('click', function () {
                    navigator.clipboard && navigator.clipboard.writeText($('#ccoDetailText').text());
                    $(this).text('คัดลอกแล้ว');
                });
            }
        }, buttons));
    }

    function confirmHtml(opts, code) {
        return slipHtml({
            kind: 'ask', code: code,
            operation: opts.confirmTitle,
            badge: 'รอยืนยัน',
            message: opts.confirmDetail || 'ยืนยันเพื่อทำรายการนี้',
            hint: ''
        });
    }

    function toastSuccess(title, message) { /* แทนที่ด้วยกล่องเต็ม */ }

    function showActionSuccess(opts, code, res) {
        slipModal({
            kind: 'ok', code: code,
            operation: opts.confirmTitle,
            badge: 'สำเร็จ',
            message: (res && res.message) || opts.successTitle,
            hint: 'ระบบอัปเดตรายการในตารางให้แล้ว',
            detail: (res && res.detail) || ''
        }, { confirmButtonText: 'เรียบร้อย' }).then(function () { reloadCurrentPage(); });
    }

    function showActionResult(kind, opts, code, res) {
        var conf = {
            warning: { badge: 'ไม่สำเร็จ',
                       hint: 'รายการยังไม่ถูกเปลี่ยนแปลง กดซ้ำก็จะได้ผลเดิม ต้องแก้ที่ต้นเหตุก่อน' },
            error:   { badge: 'ขัดข้อง',
                       hint: 'รายการยังไม่ถูกเปลี่ยนแปลง ลองกดใหม่อีกครั้งได้เลย' },
            blocked: { badge: 'ทำไม่ได้',
                       hint: 'ระบบนี้เปิดให้ดูข้อมูลอย่างเดียว หากต้องแก้ไขกรุณาแจ้งทีมผู้ดูแล' }
        }[kind];

        slipModal({
            kind: kind === 'warning' ? 'warn' : (kind === 'blocked' ? 'lock' : 'err'),
            code: code,
            operation: opts.confirmTitle,
            badge: conf.badge,
            message: (res && res.message) ||
                     'ไม่ได้รับคำตอบจากระบบ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล',
            hint: conf.hint,
            detail: (res && res.detail) || ''
        }, { confirmButtonText: 'ปิด' });
    }

    // ยิงค้นหาหน้าเดิมซ้ำ เพื่อให้สถานะในตารางอัปเดตหลังกดซ่อมสำเร็จ
    function reloadCurrentPage() {
        var current = parseInt($('#searchPager .page-item.active .page-link').data('page'), 10);
        searchForm(isNaN(current) ? 1 : current, currentPageSize(), true);
    }

    // ปุ่มซ่อมในแต่ละแถว — ผูกแบบ delegated ครั้งเดียว จะได้ไม่ต้องผูกใหม่ทุกครั้งที่วาดตาราง
            $(document).on('click', '.C100StatusClosed', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/GetStatusClosedSGFinance',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'ส่งสถานะไปยังระบบสินเชื่ออีกครั้ง',
                    confirmDetail: 'ใช้เมื่อใบคำขอปิดงานแล้วแต่สถานะยังไม่ไปถึงระบบสินเชื่อ ระบบจะส่งข้อมูลชุดเดิมซ้ำอีกครั้ง',
                    successTitle: 'ส่งสถานะแล้ว'
                });
            });

            $(document).on('click', '.GenEsignature', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/GenEsignature',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'สร้างลิงก์ลงนามใหม่',
                    confirmDetail: 'ลูกค้าจะได้รับลิงก์ลงนามสัญญาอันใหม่ ลิงก์เดิมจะใช้ไม่ได้',
                    successTitle: 'สร้างลิงก์ลงนามใหม่แล้ว'
                });
            });

            $(document).on('click', '.GetAddTNewSalesNewSGFinance', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/GetAddTNewSalesNewSGFinance',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'ส่งรายการขายอีกครั้ง',
                    confirmDetail: 'ใช้เมื่อลูกค้ารับสินค้าแล้วแต่รายการขายยังไม่ขึ้นในระบบ',
                    successTitle: 'ส่งรายการขายแล้ว'
                });
            });

            $(document).on('click', '.FixDuplicateContract', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/FixDuplicateContract',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'แก้ปัญหาสัญญาซ้ำ',
                    confirmDetail: 'ใบคำขอนี้มีสัญญามากกว่า 1 ใบ ระบบจะย้ายสัญญาที่ยังไม่ลงนามออกไป ' +
                                   'เหลือไว้เฉพาะสัญญาที่ใช้งานจริง สัญญาที่ลงนามแล้วจะไม่ถูกแตะต้อง',
                    successTitle: 'แก้สัญญาซ้ำแล้ว'
                });
            });

            $(document).on('click', '.RegisIMEI', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/RegisIMEI',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'ลงทะเบียนเครื่องกับระบบ',
                    confirmDetail: 'ส่งหมายเลขเครื่องของใบคำขอนี้ไปลงทะเบียน กดซ้ำได้หากครั้งก่อนไม่สำเร็จ',
                    successTitle: 'ลงทะเบียนเครื่องแล้ว'
                });
            });

            $(document).on('click', '.LinkPayment', function () {

                var data = {
                    ApplicationCode: $(this).data("applicationcode")
                };

                Swal.fire({
                    title: "ยืนยันส่งลิงค์ชำระเงิน?",
                    showCancelButton: true,
                    confirmButtonText: "ยืนยัน",
                    cancelButtonText: "ออก",
                }).then((result) => {
                    /* Read more about isConfirmed, isDenied below */
                    if (result.isConfirmed) {

                        var formData = $(this).serialize(); // Serialize form data
                        $.ajax({
                            url: "./Home/LinkPayment", // Action URL
                            type: "POST", // Method (POST in this case)
                            contentType: 'application/json',
                            data: JSON.stringify(data),
                            success: function (result) {
                                if (result.statusCode == "PASS") {
                                    Swal.fire({
                                        title: "ส่งลิงค์ชำระเงินสำเร็จ!",
                                        text: "ส่งลิงค์ชำระเงินสำเร็จ",
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

    // แบ่งหน้าที่ฝั่ง server — คลิกเลขหน้า/เปลี่ยนจำนวนต่อหน้า จะยิงค้นหาใหม่เฉพาะหน้านั้น
    $(document).on('click', '.page-nav', function (e) {
        e.preventDefault();
        if ($(this).closest('.page-item').hasClass('disabled')) { return; }
        searchForm(parseInt($(this).data('page'), 10), currentPageSize());
    });

    $(document).on('change', '#pageSizeSelect', function () {
        searchForm(1, parseInt($(this).val(), 10));
    });

    $(document).on('click', '#btnExportSearch', function (e) {
        e.preventDefault();
        exportSearch();
    });

    // อ่านสดทันที ข้าม cache
    $(document).on('click', '#btnRefreshNow', function (e) {
        e.preventDefault();
        var current = parseInt($('#searchPager .page-item.active .page-link').data('page'), 10);
        searchForm(isNaN(current) ? 1 : current, currentPageSize(), true);
    });

    // อัปเดตอัตโนมัติ — ทำให้หน้านี้เป็นกระดานเฝ้าดูจริง ๆ แทนที่จะเป็นภาพนิ่ง
    var autoTimer = null;
    $(document).on('change', '#autoRefresh', function () {
        if (autoTimer) { clearInterval(autoTimer); autoTimer = null; }
        if ($(this).is(':checked')) {
            autoTimer = setInterval(function () {
                if (document.hidden) { return; }   // ไม่ต้องยิงตอนผู้ใช้ไม่ได้ดูหน้านี้
                var cur = parseInt($('#searchPager .page-item.active .page-link').data('page'), 10);
                searchForm(isNaN(cur) ? 1 : cur, currentPageSize(), true);
            }, 30000);
        }
    });

    var sortState = { sort: 'date', dir: 'desc' };

    // กดหัวคอลัมน์เพื่อเรียง — กดซ้ำคอลัมน์เดิมสลับขึ้น/ลง
    $(document).on('click', 'th.sortable', function () {
        var col = $(this).data('sort');
        if (sortState.sort === col) {
            sortState.dir = sortState.dir === 'asc' ? 'desc' : 'asc';
        } else {
            sortState.sort = col;
            sortState.dir = col === 'date' ? 'desc' : 'asc';
        }
        searchForm(1, currentPageSize());
    });

    function currentPageSize() {
        var v = parseInt($('#pageSizeSelect').val(), 10);
        return isNaN(v) ? 5 : v;
    }

    // ดาวน์โหลดผลการค้นหาทั้งหมด (ไม่ใช่เฉพาะหน้าที่แสดง) — ให้เบราว์เซอร์จัดการไฟล์เอง
    function exportSearch() {
        var $tmp = $('<form>', { method: 'POST', action: '/Home/ExportSearch' }).hide();
        $('#searchForm').serializeArray().forEach(function (kv) {
            $('<input>', { type: 'hidden', name: kv.name, value: kv.value }).appendTo($tmp);
        });
        $tmp.appendTo('body').submit().remove();
    }

    function searchForm(page, pageSize, noCache) {

        checkSession();

        $('.loaddong').css('display', 'block');

        var formData = $('#searchForm').serialize();
        formData += '&page=' + (page && page > 0 ? page : 1);
        formData += '&pageSize=' + (pageSize && pageSize > 0 ? pageSize : 5);
        // หลังกดปุ่มซ่อมสำเร็จ ต้องอ่านค่าสด ไม่งั้นจะเห็นสถานะเดิมที่ยังค้างอยู่ใน cache
        if (noCache) { formData += '&noCache=true'; }
        formData += '&sort=' + encodeURIComponent(sortState.sort) + '&dir=' + encodeURIComponent(sortState.dir);

        $.ajax({
            url: $('#searchForm').attr('action'),
            type: $('#searchForm').attr('method'),
            data: formData,
            dataType: 'json',
            success: function (res) {
                $('.loaddong').css('display', 'none');
                renderResults(res);
            },
            error: function (xhr) {
                $('.loaddong').css('display', 'none');
                if (xhr.status === 401) { window.location.href = '/Login'; return; }
                var msg = (xhr.responseJSON && xhr.responseJSON.message)
                    || 'ค้นหาไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล';
                showResultsAlert('danger', msg);
                hideResults();
            }
        });
    }

    // ---------- วาดผลลัพธ์จาก JSON ----------

    function esc(v) {
        return $('<div>').text(v == null ? '' : String(v)).html();
    }

    function badge(text, ok) {
        return '<span class="badge ' + (ok ? 'bg-success' : 'bg-warning') + '"' +
               (ok ? '' : ' style="color:#000"') + '>' + esc(text) + '</span>';
    }

    function fixIcon(cls, code, title) {
        return ' <i class="' + cls + ' fa-solid fa-paper-plane row-fix" data-applicationcode="' +
               esc(code) + '" title="' + esc(title) + '" role="button" tabindex="0"></i>';
    }

    function showResultsAlert(kind, msg) {
        $('#searchAlert').html('<div class="alert alert-' + kind + '" role="alert">' + esc(msg) + '</div>');
    }

    function hideResults() {
        $('#searchToolbar').prop('hidden', true);
        $('#searchTableWrap').prop('hidden', true);
        $('#searchPager').prop('hidden', true).empty();
        $('#searchTableBody').empty();
    }

    var rowsByCode = {};

    function renderResults(res) {
        var rows = (res && res.data) || [];
        rowsByCode = {};
        rows.forEach(function (r) { if (r.applicationCode) { rowsByCode[r.applicationCode] = r; } });
        var meta = (res && res.meta) || {};

        $('#searchAlert').empty();

        if (!rows.length) {
            hideResults();
            showResultsAlert('secondary', 'ไม่พบใบคำขอตามเงื่อนไขนี้ ลองขยายช่วงวันที่ หรือตรวจสอบเลขที่ใบคำขออีกครั้ง');
            return;
        }

        var html = rows.map(rowHtml).join('');
        $('#searchTableBody').html(html);
        $('#searchTableWrap').prop('hidden', false);

        renderToolbar(meta);
        renderPager(meta);
        renderSortIndicator(meta);
        $('#autoRefresh').prop('checked', autoTimer !== null);
    }

    function rowHtml(r) {
        // สีของป้ายสถานะดูที่ "สถานะ" อย่างเดียว — CLOSED คือปิดงานแล้ว ต้องเป็นเขียว
        // (เดิมผูกสีไว้กับเงื่อนไขของปุ่มส่งสถานะซ้ำ ใบที่ CLOSED แล้วแต่ยังไม่ครบทุกขั้น
        //  เลยขึ้นเป็นเหลือง ทั้งที่สถานะจริงคือปิดงานแล้ว)
        var statusCell = badge(r.applicationStatusId, r.applicationStatusId === 'CLOSED')
            + (r.canPushStatusClosed
                ? fixIcon('C100StatusClosed', r.applicationCode, 'ส่งสถานะ CLOSED ไปปลายทางอีกครั้ง')
                : '');

        var contractCell =
            '<div><span class="cell-label">จำนวนสัญญา</span> ' + badge(r.numDoc, r.numDoc === 'ปกติ') +
                (r.canFixDuplicateContract ? fixIcon('FixDuplicateContract', r.applicationCode, 'ซ่อมสัญญาซ้ำ — เปลี่ยนเลขที่เอกสารของใบที่ยังไม่ลงนามเสร็จให้เติม _D') : '') + '</div>' +
            '<div><span class="cell-label">สถานะสัญญา</span> ' + badge(r.signedStatus, r.signedStatus === 'เรียบร้อย') +
                (r.canGenEsignature ? fixIcon('GenEsignature', r.applicationCode, 'สร้างลิงก์ e-signature ใหม่') : '') + '</div>' +
            '<div><span class="cell-label">รับสินค้า</span> ' + badge(r.statusReceived, r.statusReceived === 'รับสินค้าแล้ว') + '</div>';

        var regisIcon = r.canRegisImei
            ? fixIcon('RegisIMEI', r.applicationCode, 'ลงทะเบียนเครื่อง')
            : (r.regisBlockedReason ? blockedIcon(r.regisBlockedReason) : '');

        var checkCell =
            '<div><span class="cell-label">ลงทะเบียนเครื่อง</span> ' + badge(r.numRegis, r.numRegis === 'เรียบร้อย') + regisIcon + '</div>' +
            '<div><span class="cell-label">NewSale</span> ' + badge(r.newNum, r.newNum === 'เรียบร้อย') +
                (r.canRepushNewSale ? fixIcon('GetAddTNewSalesNewSGFinance', r.applicationCode, 'ส่ง NewSale ไปปลายทางอีกครั้ง') : '') + '</div>' +
            '<div><span class="cell-label">NewPayment</span> ' + badge(r.payNum, r.payNum === 'เรียบร้อย') + '</div>';

        return '<tr>' +
            '<td class="col-open"><a class="open-app" href="/Home/FormCancel?ApplicationCode=' + encodeURIComponent(r.refCode || '') +
                '" title="เปิดใบคำขอ"><i class="fa-solid fa-folder-open"></i></a></td>' +

            // วันที่สร้างใบคำขอ
            '<td class="nw">' +
                '<div class="v-strong">' + esc(r.applicationDate) + '</div>' +
                '<div><span class="cell-label">เลขที่ใบคำขอ</span> ' + esc(r.applicationCode) + '</div>' +
                '<div class="cell-label">' + esc(r.refCode) + '</div>' +
            '</td>' +

            // เอกสาร
            '<td>' +
                '<div><span class="cell-label">เลขที่สัญญา</span> <span class="v-strong">' + esc(r.accountNo) + '</span></div>' +
                '<div><span class="cell-label">เลขบัตรประชาชน</span> ' + esc(r.customerId) + '</div>' +
                '<div><span class="cell-label">ชื่อลูกค้า</span> ' + esc(r.customerName) + '</div>' +
                '<div><span class="cell-label">เบอร์โทรศัพท์</span> ' + esc(r.customerMobile) + '</div>' +
            '</td>' +

            // สาขา
            '<td>' +
                '<div class="clip" title="' + esc(r.saleDepName) + '">' + esc(r.saleDepName) + '</div>' +
                '<div><span class="cell-label">รหัสสาขา</span> ' + esc(r.saleDepCode) + '</div>' +
                '<div><span class="cell-label">พนักงานขาย</span> ' + esc(r.saleName) + '</div>' +
                '<div><span class="cell-label">เบอร์พนักงานขาย</span> ' + esc(r.saleTelephoneNo) + '</div>' +
            '</td>' +

            // ชื่อสินค้าและหมายเลขสินค้า
            '<td>' +
                '<div class="clip" title="' + esc(r.productModelName) + '">' + esc(r.productModelName) + '</div>' +
                '<div><span class="cell-label">Serial / IMEI</span> <span class="v-strong">' + esc(r.productSerialNo) + '</span></div>' +
                '<div class="cell-label">' + esc(r.loanTypeCate) + ' · ' + esc(r.ouCode) + '</div>' +
            '</td>' +

            '<td class="nw">' + statusCell + '</td>' +
            '<td class="nw">' + contractCell + '</td>' +
            '<td class="nw">' + checkCell + '</td>' +
        '</tr>';
    }

    // ไอคอนจาง ๆ กดไม่ได้ พร้อมบอกเหตุผล — ดีกว่าไม่แสดงอะไรเลยแล้วผู้ใช้สงสัยว่าปุ่มหายไปไหน
    function blockedIcon(reason) {
        return ' <i class="fa-solid fa-paper-plane row-fix-off" title="' + esc(reason) + '"></i>';
    }

    function renderSortIndicator(meta) {
        if (meta.sort) { sortState.sort = meta.sort; }
        if (meta.dir) { sortState.dir = meta.dir; }
        $('th.sortable').removeClass('sorted-asc sorted-desc');
        $('th.sortable[data-sort="' + sortState.sort + '"]')
            .addClass(sortState.dir === 'asc' ? 'sorted-asc' : 'sorted-desc');
    }

    function renderToolbar(meta) {
        var first = meta.total === 0 ? 0 : ((meta.page - 1) * meta.pageSize) + 1;
        var last = Math.min(meta.page * meta.pageSize, meta.total);
        // บอกเสมอว่าข้อมูลอ่านมาเมื่อไร — ระบบนี้สถานะวิ่งตลอด ผู้ใช้ต้องรู้ว่ากำลังดูของสดหรือของค้าง
        var age = meta.ageSeconds || 0;
        var stamp = age <= 1
            ? '<span class="data-fresh">ข้อมูลสด ' + esc(meta.generatedAt || '') + '</span>'
            : '<span class="data-stale">ข้อมูล ณ ' + esc(meta.generatedAt || '') + ' (' + age + ' วินาทีที่แล้ว)</span>';

        $('#resultsCount').html(
            'แสดง <b>' + first.toLocaleString() + '</b>–<b>' + last.toLocaleString() + '</b> ' +
            'จาก <b>' + meta.total.toLocaleString() + '</b> รายการ ' +
            '<span class="text-muted">(หน้า ' + meta.page + ' จาก ' + meta.totalPages + ')</span> ' +
            stamp
        );

        var $sel = $('#pageSizeSelect');
        if ($sel.children().length !== (meta.pageSizes || []).length) {
            $sel.empty();
            (meta.pageSizes || [5, 10, 20, 50, 100]).forEach(function (n) {
                $sel.append($('<option>', { value: n, text: n }));
            });
        }
        $sel.val(meta.pageSize);
        $('#searchToolbar').prop('hidden', false);
    }

    function renderPager(meta) {
        var total = meta.totalPages || 0;
        if (total < 1) { $('#searchPager').prop('hidden', true).empty(); return; }

        var cur = meta.page;

        function item(label, target, disabled, active, title) {
            return '<li class="page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '') + '">' +
                   '<a class="page-link page-nav" href="#" data-page="' + target + '"' +
                   (title ? ' title="' + title + '"' : '') + '>' + label + '</a></li>';
        }
        function gap() { return '<li class="page-item disabled"><span class="page-link">…</span></li>'; }

        // หน้าต่างเลข 5 ตัวรอบหน้าปัจจุบัน แล้วต่อด้วย … และหน้าสุดท้ายเสมอ
        var from = Math.max(1, cur - 2);
        var to = Math.min(total, from + 4);
        from = Math.max(1, to - 4);

        var html = '<ul class="pagination pagination-sm justify-content-center flex-wrap mt-3">';
        html += item('«', 1, cur === 1, false, 'หน้าแรก');
        html += item('‹', cur - 1, cur === 1, false, 'ก่อนหน้า');

        if (from > 1) {
            html += item(1, 1, false, false);
            if (from > 2) { html += gap(); }
        }
        for (var p = from; p <= to; p++) { html += item(p, p, false, p === cur); }
        if (to < total) {
            if (to < total - 1) { html += gap(); }
            html += item(total, total, false, false);
        }

        html += item('›', cur + 1, cur === total, false, 'ถัดไป');
        html += item('»', total, cur === total, false, 'หน้าสุดท้าย');
        html += '</ul>';

        $('#searchPager').html(html).prop('hidden', false);
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