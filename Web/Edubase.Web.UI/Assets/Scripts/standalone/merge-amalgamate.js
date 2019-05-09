﻿var mergersApp = new Vue({
    el: '#mergers-app',
    data: {
        localAuthorities: localAuthorities,
        types: types,
        phases: phases,
        typeMap: type2PhaseMap,

        mergerType: '',
        mergerTypeConfirmed: false,
        leadEstab: '',
        linkedEstab1: '',
        linkedEstab2: '',
        linkedEstab3: '',
        mergerEstabs: [],
        validMergeUrns: false,
        amalgamationEstabs: [],

        amalgamatedEstab1: '',
        amalgamatedEstab2: '',
        amalgamatedEstab3: '',
        amalgamatedEstab4: '',
        duplicateUrnsError: false,
        mergeDateDay: '',
        mergeDateMonth: '',
        mergeDateYear: '',
        newName: '',
        typeId: '',
        laId: '',
        phaseId: '',


        mergerTypeError: false,
        mergeDateError: false,
        mergeLengthError: false,
        mergerComplete: false,

        leadEstabError: false,
        leadEstabLengthError: false,
        leadEstabUrnCheckDone: false,
        leadEstabValid: false,
        leadEstabErrorMessage: '',

        linkedEstab1Error: false,
        linkedEstab2Error: false,
        linkedEstab3Error: false,
        linkedEstabError: false,
        linkedEstabUrnCheckDone: false,

        linkedEstab1Valid: false,
        linkedEstab2Valid: false,
        linkedEstab3Valid: false,

        amalgamatedEstab1Error: false,
        amalgamatedEstab2Error: false,
        amalgamatedEstab3Error: false,
        amalgamatedEstab4Error: false,
        amalgamateUrnError: false,
        amalgamationLengthError: false,

        amalgamatedEstab1Valid: false,
        amalgamatedEstab2Valid: false,
        amalgamatedEstab3Valid: false,
        amalgamatedEstab4Valid: false,

        nameError: false,
        typeError: false,
        phaseError: false,
        laError: false,

        estabLookup: '/api/establishment/{0}',
        commitApi: '/api/amalgamate-merge',

        commitErrors: '',
        presentExitWarning: false,
        completeAmalgamation: false,
        amalgUrn: '',
        exitUrl: '',
        isProcessing: false,
        apiError: {},

        amalEstab1Empty: false,
        amalEstab1Invalid: false,
        amalEstab1UrnChecked: false,
        amalEstab1NoMatch: false,

        amalEstab2Empty: false,
        amalEstab2Invalid: false,
        amalEstab2UrnChecked: false,
        amalEstab2NoMatch: false,

        amalEstab3Empty: false,
        amalEstab3Invalid: false,
        amalEstab3UrnChecked: false,
        amalEstab3NoMatch: false,

        amalEstab4Empty: false,
        amalEstab4Invalid: false,
        amalEstab4UrnChecked: false,
        amalEstab4NoMatch: false,

        leadEstabEmpty: false,
        leadEstabInvalid: false,
        leadEstabUrnChecked: false,
        leadEstabNoMatch: false,

        linkedEstab1Empty: false,
        linkedEstab1Invalid: false,
        linkedEstab1UrnChecked: false,
        linkedEstab1NoMatch: false,

        linkedEstab2Empty: false,
        linkedEstab2Invalid: false,
        linkedEstab2UrnChecked: false,
        linkedEstab2NoMatch: false,

        linkedEstab3Empty: false,
        linkedEstab3Invalid: false,
        linkedEstab3UrnChecked: false,
        linkedEstab3NoMatch: false
    },
    created: function () {
        this.populateSelect('new-establishment-type', this.types);
        this.populateSelect('LocalAuthorityId', this.localAuthorities);
        this.blockExits();
    },
    computed: {
        displayDate: function () {
            return this.mergeDateDay + '/' + this.mergeDateMonth + '/' + this.mergeDateYear;
        },
        showGlobalError: function () {
            return (
                this.mergerTypeError ||
                    this.amalgamateUrnError ||
                    this.leadEstabError ||
                    this.linkedEstabError ||
                    this.amalgamationLengthError ||
                    this.nameError ||
                    this.phaseError ||
                    this.typeError ||
                    this.laError ||
                    this.mergeDateError ||
                    this.mergeLengthError ||
                    this.duplicateUrnsError ||
                    this.commitErrors ||
                    this.leadEstabEmpty ||
                    this.leadEstabInvalid ||
                    this.leadEstabNoMatch ||
                    this.linkedEstab1Empty ||
                    this.linkedEstab1Invalid ||
                    this.linkedEstab1NoMatch ||
                    this.linkedEstab2Invalid ||
                    this.linkedEstab2NoMatch ||
                    this.linkedEstab3Invalid ||
                    this.linkedEstab3NoMatch ||
                    this.amalEstab1Empty ||
                    this.amalEstab1Invalid ||
                    this.amalEstab1NoMatch ||
                    this.amalEstab2Empty ||
                    this.amalEstab2Invalid ||
                    this.amalEstab2NoMatch ||
                    this.amalEstab3Invalid ||
                    this.amalEstab3NoMatch ||
                    this.amalEstab4Invalid ||
                    this.amalEstab4NoMatch
            );
        },
        schoolDetailUrl: function () {
            return "/Establishments/Establishment/Details/" + this.leadEstab;
        },
        amalgUrl: function () {
            return '/Establishments/Establishment/Details/' + this.amalgUrn;
        },
        leadEstablishmentName: function () {
            var self = this;
            if (self.validMergeUrns && self.mergerType === 'merger') {
                var leadName = self.mergerEstabs.filter(function (estab) {
                    return estab.urn === Number(self.leadEstab);
                })[0].name;

                return leadName;
            }
            return '';
        },
        selectedEstablishmentType: function () {
            var self = this;
            if (self.typeId !== '') {
                var typeName = types.filter(function(t) {
                    return t.id == self.typeId;
                })[0].name;

                return typeName;
            }
        },
        addedUrns: function() {
            if (this.mergerType === 'merger') {
                return [this.leadEstab, this.linkedEstab1, this.linkedEstab2, this.linkedEstab3];
            }
            return [this.amalgamatedEstab1, this.amalgamatedEstab2, this.amalgamatedEstab3, this.amalgamatedEstab4];
        }
    },
    methods: {
        hasDuplicateUrn: function() {
            var arr = this.addedUrns.sort();
            for (var i = 0, len = arr.length; i < len; i++) {
                if (arr[i] !== '' && arr[i + 1] === arr[i]) {
                    return true;
                }
            }
            return false;
        },
        populateSelect: function (control, data) {
            var frag = document.createDocumentFragment();

            document.getElementById(control).options.length = 0;

            $.each(data, function (n, item) {
                var option = document.createElement('option');
                option.value = item.id;
                option.innerHTML = item.name;

                frag.appendChild(option);
            });
            document.getElementById(control).appendChild(frag);
        },
        updatePhases: function () {
            var tp = type2PhaseMap;
            var self = this;
            var validOptions = [];

            var validPhaseIds =[];
            for (var k in tp) {
                if (k === self.typeId) {
                    validPhaseIds = tp[k];
                }
            }

            for (var i = 0, len = validPhaseIds.length; i < len; i++) {
                var obj = phases.filter(function(phase) {
                    return phase.id === validPhaseIds[i];
                })[0];
                validOptions.push(obj);
            }

            this.populateSelect('new-establishment-phase', validOptions);
        },
        checkMergeType: function () {
            this.mergerTypeError = this.mergerType === '';
            if (!this.mergerTypeError) {
                this.mergerTypeConfirmed = true;
                this.clearErrors();
            } else {
                this.errorFocus();
            }
        },
        checkIfInvalid: function(component){
            return this[component].length < 5 || isNaN(this[component])
        },
        clearMergeFields: function(){
            this.leadEstabEmpty = false;
            this.leadEstabInvalid = false;
            this.leadEstabUrnChecked = false;
            this.leadEstabValid = false;
            this.leadEstabNoMatch = false;

            this.linkedEstab1Empty = false;
            this.linkedEstab1Invalid = false;
            this.linkedEstab1bUrnChecked = false;
            this.linkedEstab1Valid = false;
            this.linkedEstab1NoMatch = false;

            this.linkedEstab2Empty = false;
            this.linkedEstab2Invalid = false;
            this.linkedEstab2bUrnChecked = false;
            this.linkedEstab2Valid = false;
            this.linkedEstab2NoMatch = false;

            this.linkedEstab3Empty = false;
            this.linkedEstab3Invalid = false;
            this.linkedEstab3bUrnChecked = false;
            this.linkedEstab3Valid = false;
            this.linkedEstab3NoMatch = false;
        },
        validateMergeFields: function(){
            var self = this;
            this.clearErrors();
            this.clearMergeFields();
            this.mergerEstabs = [];

            this.duplicateUrnsError = this.hasDuplicateUrn();
            //this.errorFocus();
            if (this.duplicateUrnsError) {
                if (this.leadEstab == '') { this.leadEstabEmpty = true; }
                if (this.linkedEstab1 == '') { this.linkedEstab1Empty = true; }
                if (this.linkedEstab2 == '') { this.linkedEstab2Empty = true; }
                if (this.linkedEstab3 == '') { this.linkedEstab3Empty = true; }
                return true;
            }

            if (this.leadEstab == '') {
                this.leadEstabEmpty = true;
            } else if (this.checkIfInvalid('leadEstab')) {
                this.leadEstabInvalid = true;
            } else {
                this.urnCheck(this.leadEstab, 'leadEstab');
            }

            if (this.linkedEstab1 == '') {
                this.linkedEstab1Empty = true;
            } else if (this.checkIfInvalid('linkedEstab1')) {
                this.linkedEstab1Invalid = true;
            } else {
                this.urnCheck(this.linkedEstab1, 'linkedEstab1');
            }

            if (this.linkedEstab2 == '') {
                this.linkedEstab2Empty = true;
            } else if (this.checkIfInvalid('linkedEstab2')) {
                this.linkedEstab2Invalid = true;
            } else {
                this.urnCheck(this.linkedEstab2, 'linkedEstab2');
            }

            if (this.linkedEstab3 == '') {
                this.linkedEstab3Empty = true;
            } else if (this.checkIfInvalid('linkedEstab3')) {
                this.linkedEstab3Invalid = true;
            } else {
                this.urnCheck(this.linkedEstab3, 'linkedEstab3');
            }

            var bothChecked;
            bothChecked = window.setInterval(function () {
                if (self.leadEstabUrnChecked && self.linkedEstab1UrnChecked) {
                    if (self.leadEstabValid && self.linkedEstab1Valid && !self.showGlobalError) {
                        self.clearErrors();
                        self.validMergeUrns = true;
                    }
                    window.clearInterval(bothChecked);
                    }
                },
                100);

            this.errorFocus();
        },
        validateMergerDate: function () {
            var day = parseInt(this.mergeDateDay, 10),
                month = parseInt(this.mergeDateMonth -1, 10),
                year = parseInt(this.mergeDateYear, 10),

                dateError = false,
                months31 = [0, 2, 4, 6, 7, 9, 11],
                currentYear = new Date().getFullYear();

            if (!day || !month || !year || isNaN(day) || isNaN(month) || isNaN(year)) {
                dateError = true;
            }

            var isLeap = new Date(year, 1, 29).getMonth() === 1; // will return march for non leap years

            if (isLeap && month === 1) {
                if (day > 29) {
                    dateError = true;
                }
            } else if (month === 1) {
                if (day > 28) {
                    dateError = true;
                }
            }

            if (months31.indexOf(month - 1)) {
                if (day < 1 || day > 31) {
                    dateError = true;
                }
            } else {
                if (day < 1 || day > 30) {
                    dateError = true;
                }
            }

            if (month < 1 || month > 12) {
                dateError = true;
            }

            return dateError;
        },
        clearAmalgamationFields: function(){
            this.amalEstab1Empty = false;
            this.amalEstab1Invalid = false;
            this.amalEstab1UrnChecked = false;
            this.amalEstab1Valid = false;
            this.amalEstab1NoMatch = false;

            this.amalEstab2Empty = false;
            this.amalEstab2Invalid = false;
            this.amalEstab2bUrnChecked = false;
            this.amalEstab2Valid = false;
            this.amalEstab2NoMatch = false;

            this.amalEstab3Empty = false;
            this.amalEstab3Invalid = false;
            this.amalEstab3bUrnChecked = false;
            this.amalEstab3Valid = false;
            this.amalEstab3NoMatch = false;

            this.amalEstab4Empty = false;
            this.amalEstab4Invalid = false;
            this.amalEstab4bUrnChecked = false;
            this.amalEstab4Valid = false;
            this.amalEstab4NoMatch = false;
        },
        validateAmalgamationFields: function(){
            var self = this;
            this.clearErrors();
            this.clearAmalgamationFields();
            this.amalgamationEstabs = [];

            this.duplicateUrnsError = this.hasDuplicateUrn();
            //this.errorFocus();
            if (this.duplicateUrnsError) {
                if (this.amalgamatedEstab1 == '') { this.amalEstab1Empty = true; }
                if (this.amalgamatedEstab2 == '') { this.amalEstab2Empty = true; }
                if (this.amalgamatedEstab3 == '') { this.amalEstab3Empty = true; }
                if (this.amalgamatedEstab4 == '') { this.amalEstab4Empty = true; }
                return true;
            }

            if (this.amalgamatedEstab1 == '') {
                this.amalEstab1Empty = true;
            } else if (this.checkIfInvalid('amalgamatedEstab1')) {
                this.amalEstab1Invalid = true;
            } else {
                this.urnCheck(this.amalgamatedEstab1, 'amalEstab1');
            }

            if (this.amalgamatedEstab2 == '') {
                this.amalEstab2Empty = true;
            } else if (this.checkIfInvalid('amalgamatedEstab2')) {
                this.amalEstab2Invalid = true;
            } else {
                this.urnCheck(this.amalgamatedEstab2, 'amalEstab2');
            }

            if (this.amalgamatedEstab3 == '') {
                this.amalEstab3Empty = true;
            } else if (this.checkIfInvalid('amalgamatedEstab3')) {
                this.amalEstab3Invalid = true;
            } else {
                this.urnCheck(this.amalgamatedEstab3, 'amalEstab3');
            }

            if (this.amalgamatedEstab4 == '') {
                this.amalEstab4Empty = true;
            } else if (this.checkIfInvalid('amalgamatedEstab4')) {
                this.amalEstab4Invalid = true;
            } else {
                this.urnCheck(this.amalgamatedEstab4, 'amalEstab4');
            }

            var bothChecked;
            bothChecked = window.setInterval(function () {
                if (self.amalEstab1UrnChecked && self.amalEstab2UrnChecked) {
                    if (self.amalEstab1Valid && self.amalEstab2Valid && !self.showGlobalError) {
                        self.validMergeUrns = true;
                        self.clearErrors();
                    }
                    window.clearInterval(bothChecked);
                    }
                },
                100);

            this.errorFocus();
        },
        urnCheck: function(urn, component){
            console.log('Doing the URN check');
            var self = this;

            $.ajax({
                url: self.estabLookup.replace('{0}', urn),
                dataType: 'json',
                method: 'get',
                async: false,
                success: function (data) {
                    console.log('urnCheck - success');
                    self[component + 'Valid'] = !data.notFound;
                    console.log('urnCheck - ' + component + 'Valid: ' + !data.notFound);
                    if (self[component + 'Valid']) {
                        console.log('urnCheck - Valid URN');
                        if (self.mergerType === 'merger') {
                            self.mergerEstabs.push(data.returnValue);
                        } else {
                            self.amalgamationEstabs.push(data.returnValue);
                        }
                    }
                    self[component + 'UrnChecked'] = true;
                },
                error: function (jqxhr) {
                    console.log('urnCheck - error');
                    if (jqxhr.hasOwnProperty('responseJSON')) {
                        self.apiError = jqxhr.responseJSON;
                    }
                    self[component + 'Valid'] = false;
                    self[component + 'NoMatch'] = true;
                    self[component + 'UrnChecked'] = true;
                },
                complete: function () {
                    console.log('urnCheck - complete');
                    //self[component + 'UrnChecked'] = true;
                }
            });
        },
        validateUrn: function (urn, component) {
            var self = this;

            $.ajax({
                url: self.estabLookup.replace('{0}', urn),
                dataType: 'json',
                method: 'get',
                success: function (data) {
                    console.log('validate ' + component + ' URN: success');
                    self[component + 'Valid'] = !data.notFound;
                    if (self[component + 'Valid']) {
                        if (self.mergerType === 'merger') {
                            self.mergerEstabs.push(data.returnValue);
                        } else {
                            self.amalgamationEstabs.push(data.returnValue);
                        }
                    }
                },
                error: function (jqxhr) {
                    console.log('validate ' + component + ' URN: error');
                    if (jqxhr.hasOwnProperty('responseJSON')) {
                        self.apiError = jqxhr.responseJSON;
                    }
                    self[component + 'Valid'] = false;
                },
                complete: function () {
                    self.fieldCount--;
                    if (component == 'leadEstab') {
                        self.leadEstabUrnCheckDone = true;
                    } else {
                        self.linkedEstabUrnCheckDone = true;
                    }
                }
            });
        },
        processMerger: function () {
            var self = this;
            var postData = {};

            postData.operationType = 'merge';
            postData.MergeOrAmalgamationDate = [this.mergeDateYear, this.mergeDateMonth, this.mergeDateDay].join('-');
            postData.LeadEstablishmentUrn = this.leadEstab;
            postData.UrnsToMerge = this.mergerEstabs.map(function (estab) {
                return estab.urn;
            });

            postData.UrnsToMerge = postData.UrnsToMerge.filter(function(item) {
                return item != self.leadEstab;
            });

            this.mergeDateError = this.validateMergerDate();
            this.errorFocus();

            if (!this.mergeDateError) {
                this.validLinkDate = true;
                this.isProcessing = true;
                $.ajax({
                    url: self.commitApi,
                    method: 'post',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(postData),
                    success: function (data) {
                        if (data.hasOwnProperty('successful') && data.successful) {
                            self.mergerComplete = true;
                            self.isProcessing = false;
                            self.clearErrors();
                        }
                    },
                    error: function (jqXHR) {
                        var errObj = JSON.parse(jqXHR.responseText);
                        var errMessage = '';

                        if (jqXHR.hasOwnProperty('responseJSON')) {
                            self.apiError = jqXHR.responseJSON;
                        }

                        for (var item in errObj) {
                            if (item === 'validationEnvelope') {
                                var env = errObj[item][0].errors;
                                env.forEach(function (er) {
                                    errMessage += er.message + '<br>';
                                });
                            } else if (!errObj.validationEnvelope && item === 'errors') {
                                errMessage = errObj.errors[0].message;
                                self.validMergeUrns = false;
                            }
                        }
                        self.commitErrors = errMessage;
                        self.isProcessing = false;
                        self.errorFocus();
                    }
                });
            }
        },
        processAmalgamation: function () {
            var self = this;
            var postData = {};

            this.nameError = (this.newName.length < 1);
            this.typeError = (this.typeId === '');
            this.laError = (this.laId === '');
            this.phaseError = (this.phaseId === '');
            this.mergeDateError = this.validateMergerDate();

            this.errorFocus();

            if (!this.nameError &&
                !this.typeError &&
                !this.laError &&
                !this.phaseError &&
                !this.mergeDateError &&
                !this.duplicateUrnsError) {

                this.isProcessing = true;
                postData.operationType = 'amalgamate';
                postData.MergeOrAmalgamationDate = [this.mergeDateYear, this.mergeDateMonth, this.mergeDateDay].join('-');
                postData.UrnsToMerge = this.amalgamationEstabs.map(function (estab) {
                    return estab.urn;
                });
                postData.NewEstablishmentName = this.newName;
                postData.NewEstablishmentPhaseId = this.phaseId;
                postData.NewEstablishmentTypeId = this.typeId;
                postData.NewEstablishmentLocalAuthorityId = this.laId;


                $.ajax({
                    url: self.commitApi,
                    method: 'post',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(postData),
                    success: function (data) {
                        if (data.hasOwnProperty('successful') && data.successful) {
                            self.completeAmalgamation = true;
                            self.amalgUrn = data.response.amalgamateNewEstablishmentUrn;
                            self.isProcessing = false;
                            self.clearErrors();
                        }
                    },
                    error: function (jqXHR) {
                        var errObj = JSON.parse(jqXHR.responseText);
                        var errMessage = '';

                        if (jqXHR.hasOwnProperty('responseJSON')) {
                            self.apiError = jqXHR.responseJSON;
                        }

                        for (var item in errObj) {
                            if (item === 'validationEnvelope' && errObj.validationEnvelope) {
                                var env = errObj[item][0].errors;
                                env.forEach(function (er) {
                                    //console.log(errObj[item][0].errors);
                                    //console.log(er);
                                    errMessage += er.message + '<br>';
                                });
                            } else if (!errObj.validationEnvelope && item === 'errors') {
                                errMessage = errObj.errors[0].message;
                                self.validMergeUrns = false;
                            }
                        }
                        self.commitErrors = errMessage;
                        self.isProcessing = false;
                        self.errorFocus();
                    }
                });
            }
        },
        errorFocus: function(){
            if ($('.error-summary').length) {
                window.document.title = "Error: Amalgamations and mergers tool - GOV.UK";
                $('.error-summary').focus();
            } else {
                window.setTimeout(function(){
                    if ($('.error-summary').length) {
                        window.document.title = "Error: Amalgamations and mergers tool - GOV.UK";
                        $('.error-summary').focus();
                    }
                },500);
            }
        },
        clearErrors: function(){
            window.document.title = "Amalgamations and mergers tool - GOV.UK";
            this.mergerTypeError = false;
            this.amalgamateUrnError = false;
            this.leadEstabError = false;
            this.leadEstabLengthError = false;
            this.linkedEstabError = false;
            this.amalgamationLengthError = false;
            this.nameError = false;
            this.phaseError = false;
            this.typeError = false;
            this.laError = false;
            this.mergeDateError = false;
            this.mergeLengthError = false;
            this.duplicateUrnsError = false;
            this.commitErrors = false;
        },
        exitConfirmed: function() {
            window.location = this.exitUrl;
        },
        blockExits: function () {
            var self = this;
            $('a, [value="cancel"]').on('click', function (e) {
                self.exitUrl = $(this).attr('href');
                if ((self.mergerType === 'amalgamation' && !self.completeAmalgamation) ||
                    (self.mergerType == 'merger' && !self.mergerComplete)) {
                    e.preventDefault();
                    self.presentExitWarning = true;
                    // Focus on main CTA when modal available
                    $(document).ready(function() {
                        if ($('#modal-content').length > 0) {
                            $('#button-ok').focus();
                        }
                  });
                }
            });
        },
        restart: function () {
            if (!this.mergerTypeConfirmed) {
                window.location = '/Tools';
                return;
            }
            window.location = '/Tools/MergersTool';
        }
    }

});
