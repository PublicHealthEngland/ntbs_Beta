import Vue from 'vue';
import { getHeaders, getValidationPath } from '../helpers';
const axios = require('axios');

const ValidateInput = Vue.extend({
    props: ['model', 'property'],
    data: function() {
      return {
        hasError: false
      }},
  methods: {
    validate: function (event: FocusEvent) {
        // Our onBlur validate events happen on input fields
        const inputField = event.target as HTMLInputElement
        const newValue = inputField.value;

        axios.get(`${getValidationPath(this.$props.model)}Property?key=${this.$props.property}&value=${newValue}`, null, { headers: getHeaders() })
        .then((response: any) => {
            var errorMessage = response.data;
            this.hasError = errorMessage != '';
            this.$refs["errorField"].textContent = errorMessage;
          })
        .catch((error: any) => {
            console.log(error.response)
        });
    }
  }
});

export {
    ValidateInput
};