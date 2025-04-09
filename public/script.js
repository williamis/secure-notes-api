const apiUrl = "http://localhost:5000";
let token = "";

async function register() {
    const username = document.getElementById("reg-username").value;
    const password = document.getElementById("reg-password").value;
    const res = await fetch(`${apiUrl}/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password })
    });

    if (res.ok) {
        alert("User registered! You can now login.");
    } else {
        alert("Registration failed!");
    }
}

async function login() {
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    const res = await fetch(`${apiUrl}/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password })
    });

    if (!res.ok) {
        alert("Login failed: " + res.status);
        return;
    }

    const data = await res.json();
    if (data.token) {
        token = data.token;
        document.getElementById("auth").style.display = "none";
        document.getElementById("notes").style.display = "block";
        loadNotes();
    } else {
        alert("No token received!");
    }
}

async function loadNotes() {
    const res = await fetch(`${apiUrl}/notes`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    const notes = await res.json();
    const list = document.getElementById("notelist");
    list.innerHTML = "";
    notes.forEach(n => {
        const li = document.createElement("li");
        li.innerText = n.content;
        list.appendChild(li);
    });
}

async function addNote() {
    const content = document.getElementById("newnote").value;
    await fetch(`${apiUrl}/notes`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ content })
    });
    document.getElementById("newnote").value = "";
    loadNotes();
}