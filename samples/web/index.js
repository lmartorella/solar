
function register(app, privileged) {
    require('./solar').register(app, privileged);
    require('./garden').register(app, privileged);
}

module.exports = { register };
