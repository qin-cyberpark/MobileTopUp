(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as anonymous module.
        define(['jquery'], factory);
    } else if (typeof exports === 'object') {
        // Node / CommonJS
        factory(require('jquery'));
    } else {
        // Browser globals.
        factory(jQuery);
    }
})(function ($) {

    'use strict';

    var console = window.console || { log: function () { } };

    function CropAvatar($element) {
        this.$container = $element;

        this.$avatarView = this.$container.find('.avatar-view');
        this.$loading = this.$container.find('.loading');
        this.$avatarForm = this.$container.find('.avatar-form');
        this.$avatarBody = this.$container.find('.avatar-body');
        this.$avatarUpload = this.$avatarBody.find('.avatar-upload');
        this.$voucherNumber = this.$avatarBody.find('#voucherNumber');
        this.$avatarInput = this.$avatarBody.find('.avatar-input');
        this.$avatarSave = this.$avatarBody.find('.avatar-save');

        this.$avatarWrapper = this.$container.find('.avatar-wrapper');
        this.$btnRecognize = this.$container.find('#btnRecognize');

        this.init();
    }


    CropAvatar.prototype = {
        constructor: CropAvatar,

        support: {
            fileList: !!$('<input type="file">').prop('files'),
            blobURLs: !!window.URL && URL.createObjectURL,
            formData: !!window.FormData
        },

        init: function () {
            this.support.datauri = this.support.fileList && this.support.blobURLs;
            this.addListener();
        },

        addListener: function () {
            this.$avatarView.on('click', $.proxy(this.click, this));
            this.$avatarInput.on('change', $.proxy(this.change, this));
            this.$avatarForm.on('submit', $.proxy(this.submit, this));
            this.$btnRecognize.on('click', $.proxy(this.recognize, this));
            this.$avatarSave.on('click', $.proxy(this.initSubmit, this))
        },
        change: function () {
            var files;
            var file;

            if (this.support.datauri) {
                files = this.$avatarInput.prop('files');

                if (files.length > 0) {
                    file = files[0];

                    if (this.isImageFile(file)) {
                        if (this.url) {
                            URL.revokeObjectURL(this.url); // Revoke the old one
                        }

                        this.url = URL.createObjectURL(file);
                        this.startCropper();
                    }
                }
            } else {
                file = this.$avatarInput.val();

                if (this.isImageFile(file)) {
                    //this.syncUpload();
                }
            }
        },
        recognize: function (e) {

            this.$img.cropper('crop');

            var canvas = this.$img.cropper('getCroppedCanvas');

            var context = canvas.getContext('2d');

            var dataURL = canvas.toDataURL("image/jpeg", 0.8);
            $.each($(".voucher-number"), function (idx, ipt) {
                ipt.value = "";
            });
            $.ajax({
                type: "POST",
                url: " http://voucher.greenspot.net.nz/test/admin/recognizevouchernumber",
                data: {
                    imgBase64: dataURL
                }
            }).done(function (data) {
                if (data == 'NG') {
                    $(".voucher-number")[0].value = "not found";
                    return;
                }

                var candidates = jQuery.parseJSON(data)["candidates"];

                //order by length
                candidates = candidates.sort(function (a, b) {
                    if (a.length < b.length)
                        return 1;
                    if (a.length > b.length)
                        return -1;
                    // a must be equal to b
                    return 0;
                });

                //fill
                var len = candidates.length;
                for (var idx = 0; idx<(len<2?len:2); idx++) {
                    $(".voucher-number")[1 - idx].value = candidates[idx];
                }

            });
        },
        initSubmit: function (e) {
            var id = $(e.target).attr("target-input");            
            $("#voucherNumber").val($("#"+id).val());
            return true;
        },
        submit: function () {
            console.debug("start submit");
            if (!this.$avatarInput.val() || !this.$voucherNumber.val()) {
                return false;
            }
            console.debug(this.$voucherNumber.val());
            console.debug(this.$avatarInput.val());
            if (this.support.formData) {
                this.ajaxUpload();
                return false;
            }
        },
        rotate: function (e) {
            var data;

            if (this.active) {
                data = $(e.target).data();

                if (data.method) {
                    this.$img.cropper(data.method, data.option);
                }
            }
        },
        isImageFile: function (file) {
            if (file.type) {
                return /^image\/\w+$/.test(file.type);
            } else {
                return /\.(jpg|jpeg|png|gif)$/.test(file);
            }
        },
        ajaxUpload: function () {
            var url = this.$avatarForm.attr('action');
            var data = new FormData(this.$avatarForm[0]);
            var _this = this;

            $.ajax(url, {
                type: 'post',
                data: data,
                dataType: 'json',
                processData: false,
                contentType: false,

                beforeSend: function () {
                    _this.submitStart();
                },

                success: function (data) {
                    _this.submitDone(data);
                },

                error: function (XMLHttpRequest, textStatus, errorThrown) {
                    _this.submitFail(textStatus || errorThrown);
                },

                complete: function () {
                    _this.submitEnd();
                }
            });
        },

        submitStart: function () {
            this.$loading.fadeIn();
        },

        submitDone: function (data) {
            if ($.isPlainObject(data) && data.state === 200) {
                if (data.result) {
                    this.url = data.result;

                    if (this.support.datauri || this.uploaded) {
                        this.uploaded = false;
                        this.cropDone();
                    } else {
                        this.uploaded = true;
                        this.startCropper();
                    }

                    this.$avatarInput.val('');
                    this.msg("[" + data.voucher_id + "]" + data.voucher_brand  + " #" +data.voucher_no + " added");
                } else if (data.message) {
                    this.alert(data.message);
                }
            } else{ 
                this.alert('Failed to response');
            }
        },

        submitFail: function (msg) {
            this.alert(msg);
        },

        submitEnd: function () {
            this.$loading.fadeOut();
        },
        cropDone: function () {
            this.$avatarForm.get(0).reset();
            this.stopCropper();
        },
        startCropper: function () {
            var _this = this;

            if (this.active) {
                this.$img.cropper('replace', this.url);
            } else {
                this.$img = $('<img src="' + this.url + '">');
                this.$avatarWrapper.empty().html(this.$img);
                this.$img.cropper({
                    dragMode:'move',
                    aspectRatio: 3,
                    zoom: 10,
                    cropBoxResizable: false,
                    cropBoxMovable: false,
                    background: false,
                    guides: false,
                    center: false,
                    //autoCropArea: 1,
                    viewMode: 3,
                    minCropBoxWidth: 600,
                    minCanvasWidth:300,
                    checkOrientation:true,
                    //preview: this.$avatarPreview.selector,
                    strict: false,
                    crop: function (e) {
                        var json = [
                              '{"x":' + e.x,
                              '"y":' + e.y,
                              '"height":' + e.height,
                              '"width":' + e.width,
                              '"rotate":' + e.rotate + '}'
                        ].join();
                    }
                });
                
                this.active = true;
            }
        },

        stopCropper: function () {
            if (this.active) {
                this.$img.cropper('destroy');
                this.$img.remove();
                this.active = false;
            }
        },
        alert: function (msg) {
            var $alert = [
                  '<div class="alert alert-danger avatar-alert alert-dismissable">',
                    '<button type="button" class="close" data-dismiss="alert">&times;</button>',
                    msg,
                  '</div>'
            ].join('');

            this.$avatarForm.before($alert);
        },
        msg: function (msg) {
            var $alert = [
                  '<div class="alert alert-success avatar-alert alert-dismissable">',
                    '<button type="button" class="close" data-dismiss="alert">&times;</button>',
                    msg,
                  '</div>'
            ].join('');

            this.$avatarForm.before($alert);
        }
    };

    $(function () {
        return new CropAvatar($('#crop-avatar'));

    });
});
