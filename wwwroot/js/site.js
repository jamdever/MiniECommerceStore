$(document).ready(function () {
    $("#searchInput").on("keyup", function () {
        var query = $(this).val().trim();

        if (query.length === 0) {
            $("#searchResults").hide();
            $("#featuredProducts").show();
            return;
        }

        $.getJSON('/Home/SearchProducts', { query: query }, function (products) {            var html = "";

            if (products.length === 0) {
                html = "<p class='text-muted'>No products found.</p>";
            } else {
                $.each(products, function (i, p) {
                    html += `
                    <div class="col-md-3 mb-3">
                        <a href="/Products/Details/${p.Id}" class="text-decoration-none text-dark">
                            <div class="card h-100">
                                <div class="card-body">
                                    <h5 class="card-title">${p.Name}</h5>
                                    <p class="card-text"><strong>Price:</strong> $${p.Price.toFixed(2)}</p>
                                    <p class="card-text text-truncate">${p.Description}</p>
                                </div>
                            </div>
                        </a>
                    </div>`;
                });
            }

            $("#searchResults").html(html).show();
            $("#featuredProducts").hide();
        });
    });
});
    let lastScrollTop = 0;
const navbar = document.getElementById("mainNavbar");
const navbarHeight = navbar.offsetHeight;

window.addEventListener("scroll", function () {
    if (window.scrollY > 0) {
        // Hide navbar when not at top
        navbar.style.top = `-${navbarHeight}px`;
    } else {
        // Show navbar only at the very top
        navbar.style.top = "0";
    }
});


