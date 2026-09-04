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
            // เริ่มค้นชุดใหม่จากฟอร์มด้านบน = ล้างคำค้นในผลลัพธ์ของชุดเก่าทิ้ง
            // ("ค้นในผลลัพธ์" ผูกกับผลลัพธ์ชุดที่เห็นอยู่ พอเปลี่ยนชุดแล้วมันไม่มีความหมายอีก
            //  ถ้าปล่อยค้างไว้ ผู้ใช้จะกดค้นอะไรก็ไม่เจอ เพราะคำเก่าถูกแนบไปด้วยทุกครั้ง)
            clearQuickSearch(false);
            searchForm(1, currentPageSize());
        });

        $('#BtnClearFilters').on('click', function () {
            $('#searchForm').find('input[type="text"]').val('');
            $('#status, #StatusRegis').val('');
            $('#loanTypeCate').val('LOCKPHONE');
            $('.date').datepicker('setDate', new Date());
            clearQuickSearch(false);
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
                error: function (xhr) {
                    // เดิมเงียบสนิท ค้นหาไม่ขึ้นแล้วผู้ใช้ไม่รู้ว่าเกิดอะไรขึ้น
                    if (xhr.status === 401) { window.location.href = '/Login'; return; }
                    Swal.fire({
                        icon: 'error',
                        title: 'ค้นหาไม่สำเร็จ',
                        text: 'ตอนนี้อ่านข้อมูลไม่ได้ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล'
                    });
                }
            });
        });

        $("#btnFetch2").click(function (e) { runCancel(e, '/Home/UpdateDataCancelCLOSED', 'ยกเลิกใบคำขอแบบข้ามวัน'); });

        $("#btnFetch").click(function (e) { runCancel(e, '/Home/UpdateDataCancel', 'ยกเลิกใบคำขอ'); });

        // ยกเลิกใบคำขอ — ทำหลายระบบต่อกัน จึงต้องแสดงผลทีละขั้นให้เห็นว่าไปถึงไหนแล้ว
        function runCancel(e, url, opName) {
            e.preventDefault();

            if ($.trim($('#Remark').val()) === '' ||
                ($.trim($('#Remark').val()) === 'อื่นๆ' && $.trim($('#Other').val()) === '')) {
                Swal.fire({ icon: 'error', title: 'กรอกข้อมูลไม่ครบ', text: 'กรุณาเลือกเหตุผลการยกเลิก' });
                return;
            }

            var code = $('#ApplicationCode').val();
            var opts = { confirmTitle: opName, successTitle: opName + 'เรียบร้อย' };

            slipModal({
                kind: 'ask', code: code, operation: opName, badge: 'รอยืนยัน',
                message: 'ระบบจะยกเลิกใบคำขอนี้ทั้งในระบบสินเชื่อและระบบสัญญา บางขั้นตอนย้อนกลับไม่ได้'
            }, {
                showCancelButton: true, confirmButtonText: 'ยืนยันยกเลิกใบคำขอ',
                cancelButtonText: 'ไม่ยกเลิก', reverseButtons: true, focusCancel: true
            }).then(function (choice) {
                if (!choice.isConfirmed) { return; }

                slipWaiting(opts, code);

                $.ajax({
                    url: url, type: 'POST', data: $('#FormCancel').serialize(),
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                }).done(function (res) {
                    if (res && typeof res === 'object' && 'ok' in res) {
                        slipModal({
                            kind: res.ok ? 'ok' : 'warn', code: code, operation: opName,
                            badge: res.ok ? 'สำเร็จ' : 'ไม่สมบูรณ์',
                            message: res.message,
                            hint: res.ok ? 'ระบบอัปเดตสถานะใบคำขอแล้ว'
                                         : 'ดูรายการด้านล่างว่าขั้นไหนผ่านและขั้นไหนไม่ผ่าน',
                            steps: res.steps, detail: res.detail
                        }, { confirmButtonText: res.ok ? 'เรียบร้อย' : 'ปิด' })
                        .then(function () { if (res.ok) { window.location.href = '/'; } });
                    } else {
                        // เผื่อ endpoint เดิมที่ยังคืนข้อความดิบ
                        slipModal({
                            kind: String(res || '') === '' ? 'ok' : 'warn', code: code, operation: opName,
                            badge: String(res || '') === '' ? 'สำเร็จ' : 'ไม่สำเร็จ',
                            message: String(res || '') === '' ? 'ยกเลิกใบคำขอเรียบร้อย' : String(res)
                        }, { confirmButtonText: 'ปิด' })
                        .then(function () { if (String(res || '') === '') { window.location.href = '/'; } });
                    }
                }).fail(function (xhr) {
                    if (xhr.status === 401) { window.location.href = '/Login'; return; }
                    var r = xhr.responseJSON;
                    slipModal({
                        kind: xhr.status === 403 ? 'lock' : 'err', code: code, operation: opName,
                        badge: xhr.status === 403 ? 'ทำไม่ได้' : 'ขัดข้อง',
                        message: (r && r.message) || 'ยกเลิกไม่สำเร็จ กรุณาลองใหม่อีกครั้ง',
                        steps: r && r.steps, detail: r && r.detail
                    }, { confirmButtonText: 'ปิด' });
                });
            });
        }

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

                runFixAction({
                    url: '/Home/PostBypassCustomer',
                    form: '#FormBypassCustomer',
                    body: { ApplicationCode: $.trim($('#IdCard').val()) },
                    confirmTitle: 'ยกเว้นการตรวจสอบลูกค้า',
                    confirmDetail: 'ระบบจะข้ามการตรวจสอบลูกค้ารายนี้ เพื่อให้ทำรายการต่อได้',
                    successTitle: 'ยกเว้นการตรวจสอบลูกค้าเรียบร้อย',
                    successHint: 'กลับไปหน้าค้นหาเพื่อทำรายการต่อได้เลย',
                    goHomeOnSuccess: true
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

                runFixAction({
                    url: '/Home/PostBypassIMEI',
                    form: '#FormBypassIMEI',
                    body: { ApplicationCode: $.trim($('#Imei').val()) },
                    confirmTitle: 'ยกเว้นการตรวจสอบเครื่อง',
                    confirmDetail: 'ระบบจะข้ามการตรวจสอบหมายเลขเครื่องนี้ เพื่อให้ทำรายการต่อได้',
                    successTitle: 'ยกเว้นการตรวจสอบเครื่องเรียบร้อย',
                    successHint: 'กลับไปหน้าค้นหาเพื่อทำรายการต่อได้เลย',
                    goHomeOnSuccess: true
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

                runFixAction({
                    url: '/Home/PostChangeIMEI',
                    form: '#FormChangeIMEI',
                    body: { ApplicationCode: $.trim($('#accNo').val()) },
                    confirmTitle: 'เปลี่ยนหมายเลขเครื่อง',
                    confirmDetail: 'ระบบจะเปลี่ยนหมายเลขเครื่องของสัญญานี้เป็น ' + $.trim($('#NewImei').val()),
                    successTitle: 'เปลี่ยนหมายเลขเครื่องเรียบร้อย',
                    successHint: 'กลับไปหน้าค้นหาเพื่อตรวจสอบรายการได้เลย',
                    goHomeOnSuccess: true
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

                },
                error: function () {
                    // เดิมเงียบสนิท ปุ่มค้างอยู่แบบ Loading... ตลอดไปเมื่อเรียกไม่ถึง server
                    $('#btnLogin').prop('disabled', false).html('Login');
                    Swal.fire({
                        icon: 'error',
                        title: 'เข้าสู่ระบบไม่สำเร็จ',
                        text: 'ตอนนี้ติดต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้งในอีกสักครู่'
                    });
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

            if ($icon) {
                // เปลี่ยนจรวดเป็นวงหมุน ให้เห็นว่าแถวนี้กำลังทำงานอยู่ ไม่ใช่ปุ่มหายไป
                $icon.data('busy', true).data('icon', $icon.attr('class'))
                     .attr('class', 'fa-solid fa-spinner fa-spin row-fix is-busy');
            }

            // ระหว่างรอคำตอบจากระบบปลายทาง (บางเส้นใช้เวลาหลายวินาที) ต้องมีอะไรบอกว่ากำลังทำงาน
            var waiting = slipWaiting(opts, code);

            // ปุ่มในตารางส่งเป็น JSON ส่วนหน้าฟอร์ม (bypass/เปลี่ยนเครื่อง) ส่งเป็นฟอร์มตามเดิม
            // รองรับทั้งสองแบบ จะได้ไม่ต้องแก้ทั้ง action และ model binding ฝั่ง server
            var req = opts.form
                ? { data: $(opts.form).serialize() }
                : { contentType: 'application/json', data: JSON.stringify(opts.body) };

            $.ajax($.extend({
                url: opts.url,
                type: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }, req)).done(function (res) {
                if (res && res.ok) {
                    showActionSuccess(opts, code, res);
                } else {
                    showActionResult('warning', opts, code, res);
                }
            }).fail(function (xhr) {
                if (xhr.status === 401) { window.location.href = '/Login'; return; }
                showActionResult(xhr.status === 403 ? 'blocked' : 'error', opts, code, xhr.responseJSON, xhr.status);
            }).always(function () {
                if ($icon) {
                    $icon.data('busy', false).attr('class', $icon.data('icon') || 'fa-solid fa-paper-plane row-fix');
                }
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
                '<div class="slip-result">' +
                    (o.kind === 'wait' ? '<span class="slip-spin"></span>' : '') +
                    esc(o.message) + '</div>' +
                (o.hint ? '<div class="slip-next">' + esc(o.hint) + '</div>' : '') +
                (o.steps ? stepsHtml(o.steps) : '') +
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

    // รายการขั้นตอนของงานที่ทำหลายระบบต่อกัน (เช่น ยกเลิกใบคำขอ)
    // บอกทีละขั้นว่าอะไรผ่าน อะไรพัง อะไรข้าม และขั้นไหนย้อนกลับไม่ได้
    function stepsHtml(steps) {
        var icon = { ok: '✓', failed: '✕', skipped: '–', pending: '○' };
        var rows = steps.map(function (st) {
            return '<li class="step step-' + st.status + '">' +
                   '<span class="step-mark">' + icon[st.status] + '</span>' +
                   '<span class="step-name">' + esc(st.name) +
                     (st.irreversible && st.status === 'ok'
                        ? '<span class="step-lock" title="ขั้นนี้แก้ระบบอื่นไปแล้ว ย้อนกลับเองไม่ได้">ย้อนกลับไม่ได้</span>' : '') +
                   '</span>' +
                   (st.detail ? '<span class="step-detail">' + esc(st.detail) + '</span>' : '') +
                   '</li>';
        }).join('');
        return '<ul class="steps">' + rows + '</ul>';
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

    // ใบสรุปสถานะ "กำลังดำเนินการ" — ปิดไม่ได้จนกว่าจะมีคำตอบ กันผู้ใช้กดซ้ำหรือปิดไปกลางคัน
    function slipWaiting(opts, code) {
        Swal.fire({
            html: slipHtml({
                kind: 'wait', code: code,
                operation: opts.confirmTitle,
                badge: 'กำลังดำเนินการ',
                message: 'กำลังส่งรายการไปยังระบบ กรุณารอสักครู่',
                hint: 'อย่าปิดหน้าต่างนี้จนกว่าจะได้ผลลัพธ์'
            }),
            width: '35rem',
            showConfirmButton: false,
            allowOutsideClick: false,
            allowEscapeKey: false,
            buttonsStyling: false,
            customClass: { popup: 'slip-popup', htmlContainer: 'slip-container' },
            didOpen: function () { Swal.showLoading(); }
        });
        return true;
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
            hint: opts.successHint || 'ระบบอัปเดตรายการในตารางให้แล้ว',
            detail: (res && res.detail) || ''
        }, { confirmButtonText: 'เรียบร้อย' }).then(function () {
            // หน้าฟอร์ม (bypass / เปลี่ยนเครื่อง) ไม่มีตารางให้รีเฟรช ให้กลับหน้าค้นหาแทน
            if (opts.goHomeOnSuccess) { window.location.href = '/'; return; }
            reloadCurrentPage();
        });
    }

    function showActionResult(kind, opts, code, res) {
        var conf = {
            warning: { badge: 'ไม่สำเร็จ',
                       hint: 'รายการยังไม่ถูกเปลี่ยนแปลง กดซ้ำก็จะได้ผลเดิม ต้องแก้ที่ต้นเหตุก่อน' },
            error:   { badge: 'ขัดข้อง',
                       hint: 'รายการยังไม่ถูกเปลี่ยนแปลง ลองกดใหม่อีกครั้งได้เลย' },
            blocked: { badge: 'ทำไม่ได้',
                       hint: 'ระบบปฏิเสธรายการนี้ (403) รายการยังไม่ถูกเปลี่ยนแปลง — แจ้งทีมผู้ดูแลพร้อมเลขที่ใบคำขอ' }
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
            $(document).on('click', '.RenotifyCancel', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/RenotifyCancel',
                    body: { ApplicationCode: $icon.data('applicationcode') },
                    confirmTitle: 'แจ้งยกเลิกไปยัง e-contract อีกครั้ง',
                    confirmDetail: 'ใบคำขอนี้ยกเลิกในระบบแล้ว แต่ e-contract ยังเห็นสถานะเดิม ระบบจะแจ้งการยกเลิกไปให้ใหม่',
                    successTitle: 'แจ้งยกเลิกไปยัง e-contract แล้ว'
                });
            });

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

            $(document).on('click', '.CloseDuplicateDraft', function () {
                var $icon = $(this);
                runFixAction({
                    $icon: $icon,
                    url: '/Home/CloseDuplicateDraft',
                    body: { ApplicationID: $icon.data('applicationid') },
                    confirmTitle: 'ปิดใบร่างซ้ำ',
                    confirmDetail: 'REQ นี้มีใบร่าง (DRAFT) ซ้ำที่ไม่มีเลขใบคำขอค้างอยู่ ระบบจะปิดเฉพาะใบร่างนี้ ' +
                                   'โดยคงใบจริงที่เดินหน้าแล้วไว้ ปิดได้ต่อเมื่อมีใบจริงของ REQ นี้อยู่ในระบบแล้วเท่านั้น',
                    successTitle: 'ปิดใบร่างซ้ำแล้ว'
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
                // เดิม URL เขียนเป็น "./Home/LinkPayment" ซึ่งพังถ้าเรียกจากหน้าที่ไม่ใช่ root
                runFixAction({
                    $icon: $(this),
                    url: '/Home/LinkPayment',
                    body: { ApplicationCode: $(this).data('applicationcode') },
                    confirmTitle: 'ส่งลิงก์ชำระเงินให้ลูกค้า',
                    confirmDetail: 'ระบบจะส่ง SMS พร้อมลิงก์ชำระเงินไปยังเบอร์ของลูกค้า',
                    successTitle: 'ส่งลิงก์ชำระเงินแล้ว'
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

    // ---------- ค้นในผลลัพธ์ ----------
    //
    // ตัวอักษรเดียวไม่ยิง — ในผลลัพธ์วันหนึ่งมีหลักร้อยถึงหลักพันแถว "ก" ตัวเดียวแทบจะตรงทุกแถว
    // ได้ผลกลับมาเท่าเดิมแต่เสียเวลารอ query ที่กวาดทั้งชุด · เริ่มค้นเมื่อครบ 2 ตัวอักษรขึ้นไป
    // ต่ำกว่านั้นถือว่า "ไม่กรอง" ผลลัพธ์จึงกลับมาเต็มชุดเสมอเมื่อผู้ใช้ลบคำทิ้ง
    var QUICK_MIN = 2;
    var QUICK_DELAY = 500;      // หน่วงให้พอพิมพ์คำไทยจบคำ ไม่ยิงกลางคำ
    var quickTimer = null;
    var quickApplied = '';      // คำที่ยิงไปแล้วจริง ๆ — ใช้เทียบว่าควรยิงซ้ำไหม
    var lastRes = null;         // ผลค้นหาชุดล่าสุด — เก็บไว้ re-render ตอนสลับ "เฉพาะใบซ้ำ"
    var quickTruncated = false; // server ไล่ดูไม่ครบทั้งชุดเพราะผลลัพธ์ใหญ่เกินเพดาน
    var quickScanned = 0;       // ไล่ดูไปกี่แถว

    function quickRaw() { return String($('#quickSearch').val() || '').trim(); }

    /// คำที่จะส่งไป server จริง — ต่ำกว่าขั้นต่ำถือว่าไม่ได้กรอง
    function quickEffective() {
        var v = quickRaw();
        return v.length >= QUICK_MIN ? v : '';
    }

    /// อัปเดตปุ่มล้างและข้อความใต้ช่อง ให้ผู้ใช้รู้ว่าตอนนี้ระบบกำลังรออะไรอยู่
    function renderQuickState() {
        var raw = quickRaw();
        $('#quickSearchClear').prop('hidden', raw === '');

        var $hint = $('#quickSearchHint').removeClass('is-empty');
        if (raw !== '' && raw.length < QUICK_MIN) {
            $hint.text('พิมพ์อีก ' + (QUICK_MIN - raw.length) + ' ตัวอักษรจึงจะเริ่มค้น');
        } else if (quickTruncated) {
            // ชุดผลลัพธ์ใหญ่เกินกว่าจะไล่ดูครบ ต้องบอกตรง ๆ ไม่งั้นผู้ใช้จะเข้าใจว่า "ไม่มี"
            // ทั้งที่จริงคือ "ยังไม่ได้ดูถึง" — วิธีแก้คือหรี่ช่วงวันที่หรือตัวกรองด้านบนให้แคบลง
            $hint.addClass('is-empty').text(
                'ผลลัพธ์ชุดนี้ใหญ่เกินไป ค้นได้แค่ ' + quickScanned.toLocaleString() +
                ' รายการแรก — ลองหรี่ช่วงวันที่หรือตัวกรองด้านบนให้แคบลง');
        } else if (quickApplied !== '') {
            $hint.text('กรองอยู่ด้วย “' + quickApplied + '” — กด Esc เพื่อล้าง');
        } else {
            $hint.text('');
        }
    }

    /// ยิงค้นเมื่อคำที่จะใช้จริงเปลี่ยนไปจากรอบก่อนเท่านั้น
    /// (force = กด Enter — ผู้ใช้ตั้งใจสั่งเอง ให้ยิงซ้ำได้แม้คำเดิม)
    function runQuickSearch(force) {
        clearTimeout(quickTimer);
        if (!force && quickEffective() === quickApplied) { renderQuickState(); return; }
        searchForm(1, currentPageSize());
    }

    $(document).on('input', '#quickSearch', function () {
        renderQuickState();
        clearTimeout(quickTimer);
        quickTimer = setTimeout(function () { runQuickSearch(false); }, QUICK_DELAY);
    });

    $(document).on('keydown', '#quickSearch', function (e) {
        // กด Enter ให้ค้นทันที ไม่ต้องรอหน่วง
        if (e.key === 'Enter') { e.preventDefault(); runQuickSearch(true); return; }
        // กด Esc ล้างคำค้นแล้วกลับไปดูผลลัพธ์เต็มชุด
        if (e.key === 'Escape') { e.preventDefault(); clearQuickSearch(true); }
    });

    $(document).on('click', '#quickSearchClear, .quick-clear-link', function (e) {
        e.preventDefault();
        clearQuickSearch(true);
        $('#quickSearch').trigger('focus');
    });

    /// ล้างช่องค้นในผลลัพธ์
    /// rerun = ยิงค้นใหม่ให้ผลลัพธ์กลับมาเต็มชุด (ใช้ตอนผู้ใช้กดล้างเอง)
    /// ไม่ rerun = แค่ล้างค่าทิ้ง ใช้ตอนเริ่มค้นชุดใหม่จากฟอร์มด้านบน ซึ่งจะยิงค้นอยู่แล้ว
    function clearQuickSearch(rerun) {
        clearTimeout(quickTimer);
        var had = quickApplied !== '' || quickRaw() !== '';
        $('#quickSearch').val('');
        quickTruncated = false;
        if (!rerun) { quickApplied = ''; renderQuickState(); return; }
        if (had && quickApplied !== '') { searchForm(1, currentPageSize()); }
        else { quickApplied = ''; renderQuickState(); }
    }

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
    // สลับ "เฉพาะใบซ้ำ" — กรองในผลชุดเดิม ไม่ยิง server ใหม่ (badge/ปุ่มถูกคำนวณมากับผลแล้ว)
    $(document).on('change', '#dupOnly', function () {
        if (lastRes) { renderResults(null); }
    });

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
        // ช่องค้นในผลลัพธ์อยู่นอกฟอร์ม ต้องแนบเอง ไม่งั้นไฟล์ที่ได้จะไม่ตรงกับที่เห็นบนจอ
        var quick = quickEffective();
        if (quick) { $('<input>', { type: 'hidden', name: 'quickSearch', value: quick }).appendTo($tmp); }
        $tmp.appendTo('body').submit().remove();
    }

    // ลำดับของคำค้น — พิมพ์เร็ว ๆ จะมีหลายคำขอค้างอยู่พร้อมกัน และไม่รับประกันว่าจะกลับมาตามลำดับ
    // ถ้าไม่กันไว้ คำตอบของคำเก่าที่กลับมาช้าจะทับผลของคำล่าสุดที่วาดไปแล้ว
    var searchSeq = 0;

    function searchForm(page, pageSize, noCache) {

        checkSession();

        $('.loaddong').css('display', 'block');

        var formData = $('#searchForm').serialize();
        formData += '&page=' + (page && page > 0 ? page : 1);
        formData += '&pageSize=' + (pageSize && pageSize > 0 ? pageSize : 5);
        // หลังกดปุ่มซ่อมสำเร็จ ต้องอ่านค่าสด ไม่งั้นจะเห็นสถานะเดิมที่ยังค้างอยู่ใน cache
        if (noCache) { formData += '&noCache=true'; }
        formData += '&sort=' + encodeURIComponent(sortState.sort) + '&dir=' + encodeURIComponent(sortState.dir);
        // ช่องค้นในผลลัพธ์อยู่นอกฟอร์ม (อยู่คนละแถวเหนือผลลัพธ์) จึงต้องแนบเอง
        var quick = quickEffective();
        if (quick) { formData += '&quickSearch=' + encodeURIComponent(quick); }

        var seq = ++searchSeq;

        $.ajax({
            url: $('#searchForm').attr('action'),
            type: $('#searchForm').attr('method'),
            data: formData,
            dataType: 'json',
            success: function (res) {
                if (seq !== searchSeq) { return; }   // มีคำค้นใหม่แซงไปแล้ว ทิ้งคำตอบนี้
                $('.loaddong').css('display', 'none');
                quickApplied = quick;
                // ช่องค้นในผลลัพธ์โผล่ตั้งแต่ค้นครั้งแรก และอยู่ต่อไปแม้ผลลัพธ์เป็นศูนย์
                $('#quickSearchBar').prop('hidden', false);
                renderResults(res);
            },
            error: function (xhr) {
                if (seq !== searchSeq) { return; }
                $('.loaddong').css('display', 'none');
                if (xhr.status === 401) { window.location.href = '/Login'; return; }
                quickApplied = quick;
                $('#quickSearchBar').prop('hidden', false);
                var msg = (xhr.responseJSON && xhr.responseJSON.message)
                    || 'ค้นหาไม่สำเร็จ กรุณาลองใหม่อีกครั้ง หากยังไม่ได้ให้แจ้งทีมผู้ดูแล';
                showResultsAlert('danger', msg);
                hideResults();
                renderQuickState();
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

    // ป้ายสถานะใบคำขอ แยกสีตามความหมาย ไม่ใช่แค่ผ่าน/ไม่ผ่าน
    // เขียว = ปิดงานแล้ว · เทา = จบแล้วแต่ไม่ได้ขาย · เหลือง = ยังอยู่ระหว่างดำเนินการ
    function statusBadge(text) {
        var s = String(text || '').toUpperCase();
        var cls = s === 'CLOSED' ? 'bg-success'
                : (s === 'CANCELLED' || s === 'REJECTED') ? 'bg-secondary'
                : 'bg-warning';
        var dark = cls === 'bg-warning' ? ' style="color:#000"' : '';
        return '<span class="badge ' + cls + '"' + dark + '>' + esc(text) + '</span>';
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
        // เก็บผลชุดล่าสุดไว้ เพื่อ re-render ตอนสลับ "เฉพาะใบซ้ำ" โดยไม่ต้องยิง server ใหม่
        if (res) { lastRes = res; }
        var allRows = (lastRes && lastRes.data) || [];
        var dupOnly = $('#dupOnly').is(':checked');
        var rows = dupOnly ? allRows.filter(function (r) { return r.isDuplicateReq; }) : allRows;
        rowsByCode = {};
        allRows.forEach(function (r) { if (r.applicationCode) { rowsByCode[r.applicationCode] = r; } });
        var meta = (lastRes && lastRes.meta) || {};

        $('#searchAlert').empty();

        // กรอง "เฉพาะใบซ้ำ" แล้วไม่เหลืออะไรในหน้านี้ — บอกให้ชัด อย่าไปเข้าเงื่อนไข "ไม่พบคำค้น"
        if (dupOnly && !rows.length) {
            $('#searchTableBody').html('<tr class="no-match-row"><td colspan="8">' +
                'ไม่มีใบซ้ำ (REQ ซ้ำ) ในหน้านี้ — เอาเครื่องหมาย “เฉพาะใบซ้ำ” ออกเพื่อดูทั้งหมด</td></tr>');
            $('#searchTableWrap').prop('hidden', false);
            renderToolbar(meta);
            renderPager(meta);
            renderSortIndicator(meta);
            return;
        }

        quickTruncated = quickApplied !== '' && meta.quickTruncated === true;
        quickScanned = meta.quickScanned || 0;
        renderQuickState();

        if (!rows.length) {
            // คำค้นในผลลัพธ์ไม่เจอ = ตารางต้องอยู่ที่เดิม ห้ามยุบทิ้ง
            //
            // ของเดิมยุบทั้งตาราง แถบเครื่องมือ และแถบหน้า ผู้ใช้จึงต้องไปกดค้นจากฟอร์มด้านบนใหม่
            // เพื่อเรียกตารางกลับมา ซึ่งเป็นการเริ่มเงื่อนไขชุดใหม่ — เงื่อนไขที่ตั้งไว้ก่อนหน้าหายไปด้วย
            // ที่ถูกคือแค่บอกในตัวตารางว่าคำนี้ไม่ตรงกับอะไรเลย แล้วให้ลบคำค้นทิ้งตรงนั้นได้เลย
            if (quickApplied !== '') {
                $('#searchTableBody').html(
                    '<tr class="no-match-row"><td colspan="8">' +
                    'ไม่พบรายการที่ตรงกับ “' + esc(quickApplied) + '” ในผลการค้นหาชุดนี้ ' +
                    '<a href="#" class="quick-clear-link">ล้างคำค้น</a>' +
                    '</td></tr>');
                $('#searchTableWrap').prop('hidden', false);
                renderToolbar(meta);
                renderPager(meta);
                renderSortIndicator(meta);
                return;
            }

            // ไม่มีคำค้นในผลลัพธ์ = ตัวกรองด้านบนไม่เจอจริง ๆ ไม่มีตารางให้คงไว้
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
        // คำไทยใต้ป้ายสถานะ — ผู้ใช้เรียกสถานะเป็นไทยเสมอ ("ใบที่ยกเลิก" ไม่ใช่ "ใบที่ CANCELLED")
        // และช่องค้นในผลลัพธ์รับคำไทยพวกนี้ด้วย จึงต้องเห็นบนจอ ไม่งั้นไม่มีทางรู้ว่าพิมพ์คำไหนได้
        var statusThai = r.statusText
            ? '<div class="cell-label">' + esc(r.statusText) + '</div>'
            : '';

        var statusCell = statusBadge(r.applicationStatusId)
            + (r.canPushStatusClosed
                ? fixIcon('C100StatusClosed', r.applicationCode, 'ส่งสถานะ CLOSED ไปปลายทางอีกครั้ง')
                : '')
            + (r.canRenotifyCancel
                ? fixIcon('RenotifyCancel', r.applicationCode,
                          'ใบคำขอนี้ยกเลิกแล้วแต่ e-contract ยังไม่รับรู้ — กดเพื่อแจ้งยกเลิกซ้ำ')
                : '')
            + statusThai;

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
                // REQ ซ้ำ: RefCode เดียวกันมีหลายใบในผลค้นหานี้ (ใบจริง + ใบร่างขยะ) เตือนให้ CCO เห็น
                // + ปุ่มปิดเฉพาะใบร่างขยะ (DRAFT ในกลุ่มที่มีใบจริงแล้ว)
                (r.isDuplicateReq
                    ? '<div><span class="req-dup-badge" style="display:inline-block;margin-top:2px;padding:1px 7px;border-radius:10px;background:#f8d7da;color:#842029;font-size:11px;font-weight:600;" ' +
                      'title="REQ นี้มีใบคำขอ ' + r.duplicateReqCount + ' ใบในผลค้นหานี้ — ปิดใบร่างที่ไม่มีเลขใบคำขอที่ไม่ใช้ออก">⚠ REQ ซ้ำ ' + r.duplicateReqCount + ' ใบ</span>' +
                      (r.canCloseDuplicateDraft
                        ? ' <i class="CloseDuplicateDraft fa-solid fa-trash-can row-fix" data-applicationid="' + esc(r.applicationID || '') +
                          '" title="ปิดใบร่างซ้ำใบนี้ (ยกเลิกใบร่างที่ไม่มีเลขใบคำขอ เหลือใบจริงไว้ในระบบ)" role="button" tabindex="0"></i>'
                        : '') +
                      '</div>'
                    : '') +
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

        // บอกด้วยว่าตัวเลขนี้เป็นของ "หลังกรองด้วยคำค้นในผลลัพธ์" ไม่ใช่ยอดทั้งวัน
        var chip = quickApplied === '' ? ''
            : '<span class="quick-chip">กรอง: ' + esc(quickApplied) + '</span>';

        $('#resultsCount').html(
            'แสดง <b>' + first.toLocaleString() + '</b>–<b>' + last.toLocaleString() + '</b> ' +
            'จาก <b>' + meta.total.toLocaleString() + '</b> รายการ ' +
            '<span class="text-muted">(หน้า ' + meta.page + ' จาก ' + meta.totalPages + ')</span> ' +
            stamp + chip
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