// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function() {
    // Bootstrap 5 initializes dropdowns automatically
    
    // For accessibility and mobile - ensure dropdowns can be triggered by keyboard and touch
    // and that hover states work properly
    $('.dropdown-toggle').on('mouseenter', function() {
        $(this).addClass('show').attr('aria-expanded', 'true');
        $(this).next('.dropdown-menu').addClass('show');
    });
    
    $('.nav-item.dropdown').on('mouseleave', function() {
        $(this).find('.dropdown-toggle').removeClass('show').attr('aria-expanded', 'false');
        $(this).find('.dropdown-menu').removeClass('show');
    });
});
