var AuthHelper = {};

AuthHelper.initializeInactivityTimer = function(dotnetHelper) {
    var timer; 
    var activityDetected;
    
	var inactivityPeriod = 10 * 1000; // inactivity detection period in miliseconds

    //the timer will be reset whenever the user clicks the mouse or presses the keyboard
    document.onmousemove = resetTimer;
    document.onkeypress = resetTimer;

    activityDetected = false;
    //dotnetHelper.invokeMethodAsync("ActivityDetected", true);
    timer = setTimeout(reportActivity, inactivityPeriod); //600,000 milliseconds = 10 minuts


    function resetTimer() {
        activityDetected = true;
    }

    function reportActivity() {
        dotnetHelper.invokeMethodAsync("ActivityDetected", activityDetected);
        clearTimeout(timer);
        activityDetected = false;
        timer = setTimeout(reportActivity, inactivityPeriod); //600,000 milliseconds = 10 minuts
    }

}
