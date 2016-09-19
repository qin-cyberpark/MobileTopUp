var _statisticData;
var $_availPanel;
var $_form;

$(function () {
    $_availPanel = $("#panelAvailable");
    $_form = $("#formVoucher");

    //regiester event
    $(".brand-brief").click(function () {
        ChangeBrand($(this).attr("brand-type"));
    });

    $(".brand-brief").dblclick(function () {
        ShowModel("ADD", false);
    });

    $.event.special.swipe.horizontalDistanceThreshold = 100;
    $(".container").on("swipe", function (event) {
        ShowModel("ADD", false);
    });

    $("#btnAdd").click(function () {
        AddVoucher();
        $("#confirmModal").modal('show');
    });

    $("#btnSave").click(function () {
        UpdateVoucher();
    });

    $("#btnDelete").click(function () {
        DeleteVoucher();
    });

    //show statistic
    UpdateStatistic();
});

function ChangeBrand(brand) {
    $(".brand-brief").removeClass("brief-selected");
    $(".brand-brief[brand-type=" + brand + "]").addClass("brief-selected");
    ShowBrandAvailable(brand);
}

function ShowModel(operation, isConfirm, brand, sn, no) {
    //fill data field
    $("#formOperation").val(operation);

    //brand
    $("#oriBrand").val(brand ? brand : $(".brief-selected").attr("brand-type"));
    $("#brandSelector").val(brand ? brand : $(".brief-selected").attr("brand-type"));

    //top-up number
    $("#oriTopupNumber").val(no ? no : "");
    $("#topupNumber").val(no ? no : "");
    $("#topupNumberConfirm").val(no ? no : "");

    $("#oriSerialNumber").val(sn ? sn : "");
    $("#serialNumber").val(sn ? sn : "");

    //set control
    switch (operation) {
        case "ADD":
            $("#btnAdd").show();
            $("#btnSave").hide();
            break;
        case "UPDATE":
            $("#btnSave").show();
            $("#btnAdd").hide();
            break;
        default:
            $("#btnAdd").hide();
            $("#btnSave").hide();
            break;
    }

    //clear alert
    $(".alert").remove();

    //show
    $('#editModal').modal('show');

}

function ShowUpdateModal(brand, sn, no) {
    ShowModel("UPDATE", false, brand, sn, no);
}

function ShowDelModal(brand, sn, no) {
    
    if (!brand || !sn || !no) {
        return;
    }
    
    $("#delBrand").val(brand);
    $("#delTopupNumber").val(no);
    $("#delSerialNumber").val(sn);
    $("#confirmMsg").find(".delete-prompt").html([brand, "<br/>SN-", sn, "<br/>", no].join(''));

    $('#delModal').modal('show');
}

function CheckInputData(isUpdate) {
    if (!$.isNumeric($("#topupNumber").val()) || !$.isNumeric($("#topupNumberConfirm").val()) || !$.isNumeric($("#serialNumber").val())) {
        alert("Invalid value", true);
        return false;
    }

    if ($("#topupNumber").val() != $("#topupNumberConfirm").val()) {
        alert("Twice voucher number not match", true);
        return false;
    }

    if ($("#topupNumber").val().length != 12) {
        alert("Top up number should be 12-digit", true);
        return false;
    }
    if (isUpdate && $("#oriBrand").val() == $("#brandSelector").val() &&
        $("#oriTopupNumber").val() == $("#topupNumber").val() &&
        $("#oriSerialNumber").val() == $("#serialNumber").val()) {
        alert("nothing is changed", true);
        return false;
    }

    return true;
}

function AddVoucher() {
    if (!CheckInputData()) {
        return false;
    }

    var url = $_form.attr('add-url');
    var data = new FormData($_form[0]);

    $.ajax(url, {
        type: 'post',
        data: data,
        dataType: 'json',
        processData: false,
        contentType: false,
        beforeSend: function () {
            $("#btnAdd").prop("disabled", true);
        },
        complete: function () {
            $("#btnAdd").prop("disabled", false);
        },
        success: function (data) {
            if (data.added) {
                
                $("#topupNumber").val("");
                $("#topupNumberConfirm").val("");
                $("#serialNumber").val("");
                var msg = [data.voucher_brand, data.voucher_sn, "-", data.voucher_no, "added"].join(' ');
                alert(msg);
                UpdateStatistic();
                ChangeBrand(data.voucher_brand);
            } else if (data.fail) {
                alert(data.message, true);
            } else {
                alert('Failed to add voucher', true);
            }

        },

        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(textStatus || errorThrown, true);
        }
    });

    return false;
}

function DeleteVoucher() {
    var url = $("#formDelVoucher").attr('action');
    var data = new FormData($("#formDelVoucher")[0]);

    $.ajax(url, {
        type: 'post',
        data: data,
        dataType: 'json',
        processData: false,
        contentType: false,
        beforeSend: function () {
            $("#btnConfirm").prop("disabled", true);
        },
        complete: function () {
            $("#btnConfirm").prop("disabled", false);
        },
        success: function (data) {
            if (data.deleted) {

                $("#delBrand").val("");
                $("#delSerialNumber").val("");
                $("#delTopupNumber").val("");
                var msg = [data.voucher_brand, data.voucher_sn, "-", data.voucher_no, "deleted"].join(' ');
                //alertOnMain(msg);
                UpdateStatistic();
                ChangeBrand(data.voucher_brand);
                $("#delModal").modal('hide');
            } else if (data.fail) {
                alertOnMain(data.message, true);
            } else {
                alertOnMain('Failed to add voucher', true);
            }

        },

        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alertOnMain(textStatus || errorThrown, true);
        }
    });

    return false;
}

function UpdateVoucher() {
    if (!CheckInputData(true)) {
        return false;
    }

    var url = $_form.attr('update-url');
    var data = new FormData($_form[0]);

    $.ajax(url, {
        type: 'post',
        data: data,
        dataType: 'json',
        processData: false,
        contentType: false,
        beforeSend: function () {
            $("#btnSave").prop("disabled", true);
        },
        complete: function () {
            $("#btnSave").prop("disabled", false);
        },
        success: function (data) {
            if (data.updated) {
                var msg = [data.ori_voucher_brand, data.ori_voucher_sn, "-", data.ori_voucher_no, "updated to"
                            , data.voucher_brand, data.voucher_sn, "-", data.voucher_no].join(' ');
                //alert(msg);

                //brand
                $("#oriBrand").val($("#brandSelector").val());
                //top-up number
                $("#oriTopupNumber").val($("#topupNumber").val());
                $("#oriSerialNumber").val($("#serialNumber").val());

                UpdateStatistic();
                ChangeBrand(data.voucher_brand);
                $("#editModal").modal('hide');

            } else if (data.fail) {
                alert(data.message, true);
            } else {
                alert('Failed to update voucher', true);
            }

        },

        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert(textStatus || errorThrown, true);
        }
    });

    return false;
}

function UpdateStatistic() {
    $.getJSON("GetStatistic", function (data) {
        console.debug(data);
        _statisticData = data;
        $("#updatedTime").html("updated at " + _statisticData.UpdatedTime);
        ShowBrandBrief();
        //show available number of selected brand
        var brand = $(".brief-selected").attr("brand-type");
        ShowBrandAvailable(brand);
        setTimeout(UpdateStatistic, 1 * 60 * 1000);
    }).fail(function () {
        console.log("ads");
    });
}

function ShowBrandBrief() {
    $panels = $(".brand-brief");
    $.each($panels, function (idx, panel) {
        //get brand data
        var $panel = $(panel);
        var brand = $panel.attr("brand-type");
        var data = _statisticData[brand];

        //show
        $remaining = $panel.find(".remaining");
        $remaining.html(data.AVAILABLE);
        if (data.AVAILABLE <= 3) {
            $remaining.addClass("low-stock");
        } else {
            $remaining.removeClass("low-stock");
        }
        $panel.find(".sold").html(data.UNPAID + data.PAID);
        $panel.find(".hold").html(data.UNPAID);
    });
}

function ShowBrandAvailable(brand) {
    $_availPanel.empty();
    $.each(_statisticData[brand].VOUCHERS, function (idx, v) {
        var $optEdit = $('<span class="glyphicon glyphicon-edit" aria-hidden="true"></span>');
        $optEdit.click(function () {
            ShowUpdateModal(brand, v.SN, v.NO);
        });

        var $optDel = $('<span class="glyphicon glyphicon-trash" aria-hidden="true"></span>');
        $optDel.click(function () {
            ShowDelModal(brand, v.SN, v.NO);
        });

        var $opt = $('<div class="opt"></div>');
        $opt.append($optEdit).append($optDel);

        var $row = $(['<div class="col-md-12  col-xs-12 row-voucher">',
              '<div class="number">',
                  '<div class="topup-number-sm">', v.NO, '</div>',
                  '<div class="serial-number">', v.SN, '</div>',
              '</div>',
          '</div>'].join(''));
        $row.append($opt);
        $_availPanel.append($row);
    });
}

function alert(msg, isError) {
    var typeClass = 'alert-success';
    if (isError) {
        typeClass = 'alert-danger';
    }
    var $alert = ['<div class="alert ' + typeClass + ' alert-dismissable add-voucher">',
            '<button type="button" class="close" data-dismiss="alert">&times;</button>',
            msg, '</div>'].join('');

    $_form.before($alert);
}

function alertOnMain(msg, isError) {
    var typeClass = 'alert-success';
    if (isError) {
        typeClass = 'alert-danger';
    }
    var $alert = ['<div class="alert ' + typeClass + ' alert-dismissable add-voucher">',
            '<button type="button" class="close" data-dismiss="alert">&times;</button>',
            msg, '</div>'].join('');

    $_availPanel.before($alert);
}