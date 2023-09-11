function ItemsChecked(button, checkedInputs) {    
    button.attr("disabled", checkedInputs.length == 0);
}